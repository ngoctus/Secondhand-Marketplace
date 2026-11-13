using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;

namespace SecondHandMarketplace.Web.Services.Products;

/// <summary>Điểm sao, số đánh giá, số đơn thành công và khu vực hiển thị của một cửa hàng.</summary>
public sealed record ShopDisplayInfo(decimal? DiemTrungBinh, int SoDanhGia, int SoDonThanhCong, string? KhuVuc);

/// <summary>
/// Đọc phần dữ liệu thẻ sản phẩm nằm ngoài bảng SanPham.
/// Hợp đồng 4.1/4.2 của kế hoạch đặt điểm sao ở view vw_DiemCuaHang (C) và khu vực ở
/// CuaHang.MaTinhThanh/MaPhuongXa + bảng TinhThanh/PhuongXa (A). Hai schema đó chưa được gộp,
/// nên mỗi truy vấn kiểm tra COL_LENGTH/OBJECT_ID trước khi dùng: thiếu thì trả khoảng trống
/// để giao diện ghi "Chưa cập nhật" thay vì báo lỗi DB. Khi A và C xong, bỏ được các bước kiểm tra này.
/// </summary>
public interface IShopDisplayReader
{
    Task<IReadOnlyDictionary<int, ShopDisplayInfo>> LayTheoCuaHangAsync(IEnumerable<int> maCuaHang, CancellationToken ct = default);

    /// <summary>
    /// Cửa hàng thuộc tỉnh/xã đã chọn. Trả về null khi schema khu vực chưa tồn tại,
    /// phân biệt với danh sách rỗng (có schema nhưng không shop nào khớp).
    /// </summary>
    Task<List<int>?> LayCuaHangTrongKhuVucAsync(int? maTinhThanh, int? maPhuongXa, CancellationToken ct = default);

    /// <summary>Tên tỉnh/xã đã chọn để hiển thị lại cho người dùng; null khi chưa có schema khu vực.</summary>
    Task<string?> LayTenKhuVucAsync(int? maTinhThanh, int? maPhuongXa, CancellationToken ct = default);
}

public sealed class ShopDisplayReader : IShopDisplayReader
{
    private const string DieuKienCotKhuVuc =
        "COL_LENGTH('dbo.CuaHang','MaPhuongXa') IS NOT NULL AND COL_LENGTH('dbo.CuaHang','MaTinhThanh') IS NOT NULL";

    private readonly AppDbContext _db;

    public ShopDisplayReader(AppDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<int, ShopDisplayInfo>> LayTheoCuaHangAsync(IEnumerable<int> maCuaHang, CancellationToken ct = default)
    {
        var ids = maCuaHang.Where(id => id > 0).Distinct().ToList();
        var ketQua = new Dictionary<int, ShopDisplayInfo>();
        if (ids.Count == 0 || !_db.Database.IsRelational()) return ketQua;

        await using var conn = await MoKetNoiAsync(ct);
        var uyTin = await DocUyTinAsync(conn, ids, ct);
        var khuVuc = await DocKhuVucAsync(conn, ids, ct);

        foreach (var id in ids)
        {
            uyTin.TryGetValue(id, out var diem);
            khuVuc.TryGetValue(id, out var ten);
            ketQua[id] = new ShopDisplayInfo(diem.Diem, diem.SoDanhGia, diem.SoDon, ten);
        }
        return ketQua;
    }

    public async Task<List<int>?> LayCuaHangTrongKhuVucAsync(int? maTinhThanh, int? maPhuongXa, CancellationToken ct = default)
    {
        if (maTinhThanh is null && maPhuongXa is null) return [];
        if (!_db.Database.IsRelational()) return null;

        var sql = $"""
            IF NOT ({DieuKienCotKhuVuc})
            BEGIN
                SELECT 0 AS CoSchema, CAST(NULL AS int) AS MaCuaHang;
                RETURN;
            END
            SELECT 1 AS CoSchema, c.MaCuaHang
            FROM dbo.CuaHang c
            WHERE (@tinh IS NULL OR c.MaTinhThanh = @tinh)
              AND (@xa IS NULL OR c.MaPhuongXa = @xa);
            """;

        await using var conn = await MoKetNoiAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@tinh", SqlDbType.Int) { Value = (object?)maTinhThanh ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@xa", SqlDbType.Int) { Value = (object?)maPhuongXa ?? DBNull.Value });

        var ketQua = new List<int>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            if (!reader.GetBoolean(0)) return null;
            ketQua.Add(reader.GetInt32(1));
        }
        return ketQua;
    }

    public async Task<string?> LayTenKhuVucAsync(int? maTinhThanh, int? maPhuongXa, CancellationToken ct = default)
    {
        if (maTinhThanh is null && maPhuongXa is null) return null;
        if (!_db.Database.IsRelational()) return null;

        var sql = $"""
            IF NOT ({DieuKienCotKhuVuc}) OR OBJECT_ID('dbo.TinhThanh','U') IS NULL OR OBJECT_ID('dbo.PhuongXa','U') IS NULL RETURN;
            SELECT (SELECT TOP 1 TenTinhThanh FROM dbo.TinhThanh WHERE MaTinhThanh = @tinh) AS TenTinh,
                   (SELECT TOP 1 TenPhuongXa FROM dbo.PhuongXa WHERE MaPhuongXa = @xa) AS TenXa;
            """;

        await using var conn = await MoKetNoiAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@tinh", SqlDbType.Int) { Value = (object?)maTinhThanh ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@xa", SqlDbType.Int) { Value = (object?)maPhuongXa ?? DBNull.Value });

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        var tenTinh = reader.IsDBNull(0) ? null : reader.GetString(0);
        var tenXa = reader.IsDBNull(1) ? null : reader.GetString(1);
        return (tenXa, tenTinh) switch
        {
            (null, null) => null,
            (null, _) => tenTinh,
            (_, null) => tenXa,
            _ => $"{tenXa}, {tenTinh}"
        };
    }

    // ------------------------------------------------------------------ Uy tín cửa hàng (vw_DiemCuaHang của C)
    private static async Task<Dictionary<int, (decimal? Diem, int SoDanhGia, int SoDon)>> DocUyTinAsync(
        SqlConnection conn, List<int> ids, CancellationToken ct)
    {
        var sql = $"""
            IF OBJECT_ID('dbo.vw_DiemCuaHang','V') IS NOT NULL
                SELECT c.MaCuaHang, v.DiemTrungBinh, v.SoDanhGia, v.SoDonThanhCong
                FROM dbo.CuaHang c
                LEFT JOIN dbo.vw_DiemCuaHang v ON v.MaCuaHang = c.MaCuaHang
                WHERE c.MaCuaHang IN ({DanhSachId(ids)});
            """;

        var ketQua = new Dictionary<int, (decimal?, int, int)>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            ketQua[reader.GetInt32(0)] = (
                reader.IsDBNull(1) ? null : reader.GetDecimal(1),
                reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                reader.IsDBNull(3) ? 0 : reader.GetInt32(3));
        }
        return ketQua;
    }

    // ------------------------------------------------------------------ Khu vực (TinhThanh/PhuongXa của A)

    private static async Task<Dictionary<int, string?>> DocKhuVucAsync(SqlConnection conn, List<int> ids, CancellationToken ct)
    {
        var sql = $"""
            IF ({DieuKienCotKhuVuc}) AND OBJECT_ID('dbo.PhuongXa','U') IS NOT NULL
                SELECT c.MaCuaHang,
                       CASE WHEN px.TenPhuongXa IS NULL AND tt.TenTinhThanh IS NULL THEN CAST(NULL AS nvarchar(200))
                            ELSE CONCAT(ISNULL(px.TenPhuongXa, N''),
                                        CASE WHEN tt.TenTinhThanh IS NULL THEN N'' ELSE N', ' + tt.TenTinhThanh END) END AS KhuVuc
                FROM dbo.CuaHang c
                LEFT JOIN dbo.PhuongXa px ON px.MaPhuongXa = c.MaPhuongXa
                LEFT JOIN dbo.TinhThanh tt ON tt.MaTinhThanh = c.MaTinhThanh
                WHERE c.MaCuaHang IN ({DanhSachId(ids)});
            """;

        var ketQua = new Dictionary<int, string?>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            ketQua[reader.GetInt32(0)] = reader.IsDBNull(1) ? null : reader.GetString(1);
        }
        return ketQua;
    }

    private async Task<SqlConnection> MoKetNoiAsync(CancellationToken ct)
    {
        var chuoi = _db.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Thiếu chuỗi kết nối DefaultConnection.");
        var conn = new SqlConnection(chuoi);
        await conn.OpenAsync(ct);
        return conn;
    }

    // Id lấy từ chính database nên là số nguyên đã kiểm tra; ghép chuỗi để IN() chạy được một lần cho cả trang.
    private static string DanhSachId(List<int> ids) =>
        string.Join(",", ids.Select(id => id.ToString(CultureInfo.InvariantCulture)));
}
