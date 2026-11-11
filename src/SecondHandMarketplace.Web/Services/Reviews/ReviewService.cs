using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Reviews;

namespace SecondHandMarketplace.Web.Services.Reviews;

/// <summary>
/// Triển khai nghiệp vụ đánh giá người bán (C2).
///
/// Quy ước lỗi SQL của sp_TaoDanhGia (database/11_danhgia.sql):
///   50001 đơn hàng không tồn tại
///   50002 không phải người mua của đơn
///   50003 đơn chưa hoàn tất
///   50004 đơn đã được đánh giá
///   50005 số sao ngoài khoảng 1–5
///
/// Phân biệt môi trường:
///   SQL Server  → gọi sp_TaoDanhGia (toàn vẹn transaction + UNIQUE chống đua nhau).
///   InMemory    → thực thi logic tương đương bằng EF (chỉ dùng trong xUnit test).
///   Không được âm thầm chuyển sang InMemory khi SQL Server lỗi về nghiệp vụ.
/// </summary>
public sealed class ReviewService(AppDbContext db) : IReviewService
{
    // =========================================================
    // Hằng số nghiệp vụ
    // =========================================================
    private static readonly HashSet<string> HopLeTrangThai = ["VISIBLE", "HIDDEN"];

    // Mã lỗi SQL từ sp_TaoDanhGia (11_danhgia.sql)
    private const int ErrDonKhongTonTai = 50001;
    private const int ErrKhongPhaiNguoiMua = 50002;
    private const int ErrDonChuaHoanTat = 50003;
    private const int ErrDaCoNguoiDanhGia = 50004;
    private const int ErrSoSaoKhongHopLe = 50005;
    // Lỗi UNIQUE vi phạm (hai request cùng lúc)
    private const int ErrUniqueViolation1 = 2601;
    private const int ErrUniqueViolation2 = 2627;

    // =========================================================
    // 1. Tạo đánh giá
    // =========================================================
    public async Task<(bool Success, string? Error, int? MaDanhGia)> TaoDanhGiaAsync(
        int maDonHang,
        int maNguoiMua,
        byte soSao,
        string? noiDung,
        CancellationToken cancellationToken = default)
    {
        // Validation sơ bộ ở C# (SP là chốt chặn cuối)
        if (soSao is < 1 or > 5)
            return (false, "Số sao phải từ 1 đến 5.", null);

        // Chuẩn hóa nội dung: bỏ khoảng trắng, rỗng thành null
        noiDung = string.IsNullOrWhiteSpace(noiDung) ? null : noiDung.Trim();
        if (noiDung?.Length > 1000)
            return (false, "Nội dung đánh giá không được vượt quá 1 000 ký tự.", null);

        // ── SQL Server: gọi stored procedure ──────────────────
        if (db.Database.IsRelational())
            return await TaoDanhGiaQuaSPAsync(maDonHang, maNguoiMua, soSao, noiDung, cancellationToken);

        // ── InMemory (chỉ dùng trong xUnit test) ─────────────
        return await TaoDanhGiaInMemoryAsync(maDonHang, maNguoiMua, soSao, noiDung, cancellationToken);
    }

    /// <summary>Gọi sp_TaoDanhGia trên SQL Server thực.</summary>
    private async Task<(bool Success, string? Error, int? MaDanhGia)> TaoDanhGiaQuaSPAsync(
        int maDonHang, int maNguoiMua, byte soSao, string? noiDung, CancellationToken ct)
    {
        try
        {
            // sp_TaoDanhGia trả về 1 dòng: MaDanhGia
            var sql = $@"EXEC dbo.sp_TaoDanhGia
                            @MaDonHang  = {maDonHang},
                            @MaNguoiMua = {maNguoiMua},
                            @SoSao      = {soSao},
                            @NoiDung    = {(object?)noiDung ?? DBNull.Value}";

            var result = await db.Database
                .SqlQueryRaw<int>(
                    "EXEC dbo.sp_TaoDanhGia @MaDonHang, @MaNguoiMua, @SoSao, @NoiDung",
                    new SqlParameter("@MaDonHang", maDonHang),
                    new SqlParameter("@MaNguoiMua", maNguoiMua),
                    new SqlParameter("@SoSao", soSao),
                    new SqlParameter("@NoiDung", (object?)noiDung ?? DBNull.Value))
                .ToListAsync(ct);

            var maDanhGia = result.FirstOrDefault();
            return (true, null, maDanhGia);
        }
        catch (SqlException ex) when (ex.Number is ErrDonKhongTonTai or ErrKhongPhaiNguoiMua
                                               or ErrDonChuaHoanTat or ErrDaCoNguoiDanhGia
                                               or ErrSoSaoKhongHopLe
                                               or ErrUniqueViolation1 or ErrUniqueViolation2)
        {
            // 50004 / 2601 / 2627: hai request cùng lúc — cũng coi là "đã đánh giá"
            var msg = ex.Number is ErrUniqueViolation1 or ErrUniqueViolation2
                ? "Đơn hàng này đã được đánh giá."
                : ex.Message;
            return (false, msg, null);
        }
        catch (SqlException ex)
        {
            // Lỗi SQL không thuộc nghiệp vụ: trả thông báo thân thiện
            return (false, SqlLoi.ThongBao(ex), null);
        }
    }

    /// <summary>
    /// Fallback cho EF InMemory (xUnit). Mô phỏng logic sp_TaoDanhGia trong C#.
    /// KHÔNG được gọi khi đang chạy với SQL Server thật.
    /// </summary>
    private async Task<(bool Success, string? Error, int? MaDanhGia)> TaoDanhGiaInMemoryAsync(
        int maDonHang, int maNguoiMua, byte soSao, string? noiDung, CancellationToken ct)
    {
        var don = await db.DonHangs.AsNoTracking()
            .SingleOrDefaultAsync(d => d.MaDonHang == maDonHang, ct);

        if (don is null)
            return (false, "Đơn hàng không tồn tại.", null);
        if (don.MaNguoiDung != maNguoiMua)
            return (false, "Bạn không phải người mua của đơn hàng này nên không thể đánh giá.", null);
        if (don.TrangThai != "COMPLETED")
            return (false, "Chỉ được đánh giá khi đơn hàng đã hoàn tất.", null);
        if (await db.DanhGias.AnyAsync(d => d.MaDonHang == maDonHang, ct))
            return (false, "Đơn hàng này đã được đánh giá.", null);

        var entity = new DanhGia
        {
            MaDonHang  = maDonHang,
            MaNguoiMua = maNguoiMua,
            MaCuaHang  = don.MaCuaHang,
            SoSao      = soSao,
            NoiDung    = noiDung,
            TrangThai  = "VISIBLE",
            NgayTao    = DateTime.UtcNow
        };
        db.DanhGias.Add(entity);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // UNIQUE bị vi phạm (InMemory không có, nhưng để an toàn)
            foreach (var e in db.ChangeTracker.Entries<DanhGia>().Where(e => e.State == EntityState.Added).ToList())
                e.State = EntityState.Detached;
            return (false, "Đơn hàng này đã được đánh giá.", null);
        }
        return (true, null, entity.MaDanhGia);
    }

    // =========================================================
    // 2. Lấy danh sách đánh giá theo shop (có phân trang)
    // =========================================================
    public async Task<DanhSachDanhGiaTheoShopVm> LayDanhGiaTheoShopAsync(
        int maCuaHang,
        int page = 1,
        int pageSize = 5,
        bool chiLayTatCa = false,
        CancellationToken cancellationToken = default)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = db.DanhGias.AsNoTracking()
            .Where(d => d.MaCuaHang == maCuaHang);

        if (!chiLayTatCa)
            query = query.Where(d => d.TrangThai == "VISIBLE");

        var total = await query.CountAsync(cancellationToken);

        // Chỉ lấy thông tin cần thiết của người mua — không lộ Email, SoDienThoai
        var items = await query
            .OrderByDescending(d => d.NgayTao).ThenByDescending(d => d.MaDanhGia)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(d => new DanhGiaItemVm
            {
                MaDanhGia          = d.MaDanhGia,
                MaDonHang          = d.MaDonHang,
                MaCuaHang          = d.MaCuaHang,
                MaNguoiMua         = d.MaNguoiMua,
                TenNguoiMua        = d.NguoiMua.HoTen,
                AnhDaiDienNguoiMua = d.NguoiMua.AnhDaiDien,
                SoSao              = d.SoSao,
                NoiDung            = d.NoiDung,
                TrangThai          = d.TrangThai,
                NgayTao            = d.NgayTao
            })
            .ToListAsync(cancellationToken);

        // Đọc điểm trung bình tuần tự (tránh xung đột DbContext đa luồng trên SQL Server)
        var diem = await LayDiemCuaHangAsync(maCuaHang, cancellationToken);
        var tenShop = await db.CuaHangs.AsNoTracking()
            .Where(c => c.MaCuaHang == maCuaHang)
            .Select(c => c.TenShop)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return new DanhSachDanhGiaTheoShopVm
        {
            MaCuaHang     = maCuaHang,
            TenShop       = tenShop,
            DiemTrungBinh = diem?.DiemTrungBinh ?? 0m,
            SoDanhGia     = diem?.SoDanhGia ?? 0,
            Items         = items,
            Page          = page,
            PageSize      = pageSize,
            TotalItems    = total
        };
    }

    // =========================================================
    // 3. Admin ẩn / hiện đánh giá
    // =========================================================
    public async Task<(bool Success, string? Error)> DoiTrangThaiHienThiAsync(
        int maDanhGia,
        string trangThaiMoi,
        int adminId,
        CancellationToken cancellationToken = default)
    {
        trangThaiMoi = trangThaiMoi.Trim().ToUpperInvariant();
        if (!HopLeTrangThai.Contains(trangThaiMoi))
            return (false, "Trạng thái không hợp lệ. Chỉ chấp nhận VISIBLE hoặc HIDDEN.");

        var entity = await db.DanhGias.FindAsync([maDanhGia], cancellationToken);
        if (entity is null)
            return (false, "Không tìm thấy đánh giá.");

        if (entity.TrangThai == trangThaiMoi)
            return (true, null); // idempotent: đã ở trạng thái đó rồi

        entity.TrangThai = trangThaiMoi;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            return (false, SqlLoi.ThongBao(ex));
        }
    }

    // =========================================================
    // 4. Kiểm tra đơn đã đánh giá chưa (dùng bởi C3)
    // =========================================================
    public Task<bool> KiemTraDonDaDanhGiaAsync(int maDonHang, CancellationToken cancellationToken = default) =>
        db.DanhGias.AsNoTracking().AnyAsync(d => d.MaDonHang == maDonHang, cancellationToken);

    // =========================================================
    // 5. Đọc điểm shop từ vw_DiemCuaHang
    // =========================================================
    public async Task<DiemCuaHangVm?> LayDiemCuaHangAsync(int maCuaHang, CancellationToken cancellationToken = default)
    {
        var dict = await LayDiemNhieuShopAsync([maCuaHang], cancellationToken);
        return dict.TryGetValue(maCuaHang, out var vm) ? vm : null;
    }

    public async Task<Dictionary<int, DiemCuaHangVm>> LayDiemNhieuShopAsync(
        IEnumerable<int> maCuaHangs,
        CancellationToken cancellationToken = default)
    {
        var ids = maCuaHangs.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, DiemCuaHangVm>();

        // Truy vấn vw_DiemCuaHang thông qua EF (view được map bởi AppDbContext qua DiemCuaHang keyless entity)
        // Vì AppDbContext chưa map vw_DiemCuaHang thành keyless entity, ta tính tương đương:
        //   - DiemTrungBinh, SoDanhGia: từ bảng DanhGia (chỉ VISIBLE)
        //   - SoDonThanhCong: từ bảng DonHang (COMPLETED)
        // Kết quả khớp đúng định nghĩa của view (11_danhgia.sql).
        var danhGiaStats = await db.DanhGias.AsNoTracking()
            .Where(d => ids.Contains(d.MaCuaHang) && d.TrangThai == "VISIBLE")
            .GroupBy(d => d.MaCuaHang)
            .Select(g => new
            {
                MaCuaHang     = g.Key,
                SoDanhGia     = g.Count(),
                TongSao       = (int)g.Sum(d => d.SoSao)
            })
            .ToListAsync(cancellationToken);

        var donStats = await db.DonHangs.AsNoTracking()
            .Where(d => ids.Contains(d.MaCuaHang) && d.TrangThai == "COMPLETED")
            .GroupBy(d => d.MaCuaHang)
            .Select(g => new { MaCuaHang = g.Key, SoDonThanhCong = g.Count() })
            .ToListAsync(cancellationToken);

        var danhGiaDict = danhGiaStats.ToDictionary(x => x.MaCuaHang);
        var donDict     = donStats.ToDictionary(x => x.MaCuaHang);

        return ids.ToDictionary(
            id => id,
            id =>
            {
                danhGiaDict.TryGetValue(id, out var dg);
                donDict.TryGetValue(id, out var don);
                var soDanhGia = dg?.SoDanhGia ?? 0;
                var diemTrungBinh = soDanhGia > 0
                    ? Math.Round(dg!.TongSao * 1.0m / soDanhGia, 2)
                    : 0m;
                return new DiemCuaHangVm
                {
                    MaCuaHang      = id,
                    DiemTrungBinh  = diemTrungBinh,
                    SoDanhGia      = soDanhGia,
                    SoDonThanhCong = don?.SoDonThanhCong ?? 0
                };
            });
    }

    // =========================================================
    // 6. Lấy danh sách đánh giá của chính người mua (C3)
    // =========================================================
    public async Task<DanhSachDanhGiaCuaToiVm> LayDanhGiaCuaNguoiMuaAsync(
        int maNguoiMua,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = db.DanhGias.AsNoTracking()
            .Where(d => d.MaNguoiMua == maNguoiMua);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.NgayTao).ThenByDescending(d => d.MaDanhGia)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(d => new DanhGiaCuaToiItemVm
            {
                MaDanhGia = d.MaDanhGia,
                MaDonHang = d.MaDonHang,
                MaDon = d.DonHang.MaDon,
                MaCuaHang = d.MaCuaHang,
                TenShop = d.CuaHang.TenShop,
                SoSao = d.SoSao,
                NoiDung = d.NoiDung,
                TrangThai = d.TrangThai,
                NgayTao = d.NgayTao,
                TenSanPhams = d.DonHang.ChiTiet.Select(ct => ct.TenSanPham).ToList(),
                AnhSanPhamDaiDien = d.DonHang.ChiTiet.Select(ct => ct.AnhDaiDien).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return new DanhSachDanhGiaCuaToiVm
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    // =========================================================
    // 7. Lấy đánh giá của một đơn hàng (C3)
    // =========================================================
    public async Task<DanhGiaItemVm?> LayDanhGiaTheoDonHangAsync(int maDonHang, CancellationToken cancellationToken = default)
    {
        return await db.DanhGias.AsNoTracking()
            .Where(d => d.MaDonHang == maDonHang)
            .Select(d => new DanhGiaItemVm
            {
                MaDanhGia = d.MaDanhGia,
                MaDonHang = d.MaDonHang,
                MaCuaHang = d.MaCuaHang,
                MaNguoiMua = d.MaNguoiMua,
                TenNguoiMua = d.NguoiMua.HoTen,
                AnhDaiDienNguoiMua = d.NguoiMua.AnhDaiDien,
                SoSao = d.SoSao,
                NoiDung = d.NoiDung,
                TrangThai = d.TrangThai,
                NgayTao = d.NgayTao
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
