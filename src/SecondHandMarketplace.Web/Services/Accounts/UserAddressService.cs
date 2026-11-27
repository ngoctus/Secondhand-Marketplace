using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.Areas;

namespace SecondHandMarketplace.Web.Services.Accounts;

/// <summary>
/// Quản lý sổ địa chỉ (mặc định + dự phòng) của người dùng.
/// Tự đảm bảo bảng DiaChiNguoiDung tồn tại trên SQL Server để không lỗi khi chưa chạy file migration tay,
/// đồng thời luôn đồng bộ địa chỉ mặc định về cột NguoiDung.DiaChiMacDinh.
/// </summary>
public sealed class UserAddressService(AppDbContext db, IKhuVucService khuVuc) : IUserAddressService
{
    private const int MaxAddressesPerUser = 8;
    private static volatile bool _schemaEnsured;
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);

    private async Task EnsureTableAsync(CancellationToken ct)
    {
        if (_schemaEnsured || !db.Database.IsRelational()) return;

        await SchemaLock.WaitAsync(ct);
        try
        {
            if (_schemaEnsured) return;
            await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'dbo.DiaChiNguoiDung', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DiaChiNguoiDung (
        MaDiaChi        int           IDENTITY(1,1) NOT NULL CONSTRAINT PK_DiaChiNguoiDung PRIMARY KEY,
        MaNguoiDung     int           NOT NULL CONSTRAINT FK_DiaChiNguoiDung_NguoiDung FOREIGN KEY REFERENCES dbo.NguoiDung(MaNguoiDung),
        NhanDiaChi      nvarchar(60)  NULL,
        TenNguoiNhan    nvarchar(100) NOT NULL,
        SoDienThoaiNhan varchar(15)   NOT NULL,
        MaTinhThanh     int           NULL,
        MaPhuongXa      int           NULL,
        TenTinhThanh    nvarchar(100) NULL,
        TenPhuongXa     nvarchar(100) NULL,
        DiaChiChiTiet   nvarchar(300) NOT NULL,
        LaMacDinh       bit           NOT NULL CONSTRAINT DF_DiaChiNguoiDung_LaMacDinh DEFAULT (0),
        NgayTao         datetime2(3)  NOT NULL CONSTRAINT DF_DiaChiNguoiDung_NgayTao DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_DiaChiNguoiDung_NguoiDung ON dbo.DiaChiNguoiDung(MaNguoiDung, LaMacDinh DESC, MaDiaChi DESC);
END", ct);
            _schemaEnsured = true;
        }
        finally
        {
            SchemaLock.Release();
        }
    }

    public async Task<IReadOnlyList<DiaChiNguoiDung>> GetAllAsync(int userId, CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);
        return await db.DiaChiNguoiDungs.AsNoTracking()
            .Where(a => a.MaNguoiDung == userId)
            .OrderByDescending(a => a.LaMacDinh)
            .ThenByDescending(a => a.MaDiaChi)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error, DiaChiNguoiDung? Saved)> AddAsync(
        int userId, UserAddressInput input, CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);

        var (okArea, areaErr, tenTinh, tenXa) = await ResolveAreaAsync(input.MaTinhThanh, input.MaPhuongXa, cancellationToken);
        if (!okArea) return (false, areaErr, null);

        var count = await db.DiaChiNguoiDungs.CountAsync(a => a.MaNguoiDung == userId, cancellationToken);
        if (count >= MaxAddressesPerUser)
            return (false, $"Bạn chỉ có thể lưu tối đa {MaxAddressesPerUser} địa chỉ.", null);

        var shouldBeDefault = input.LaMacDinh || count == 0;
        if (shouldBeDefault)
        {
            var existingDefaults = await db.DiaChiNguoiDungs
                .Where(a => a.MaNguoiDung == userId && a.LaMacDinh)
                .ToListAsync(cancellationToken);
            foreach (var d in existingDefaults) d.LaMacDinh = false;
        }

        var entity = new DiaChiNguoiDung
        {
            MaNguoiDung = userId,
            NhanDiaChi = string.IsNullOrWhiteSpace(input.NhanDiaChi) ? null : input.NhanDiaChi.Trim(),
            TenNguoiNhan = input.TenNguoiNhan.Trim(),
            SoDienThoaiNhan = input.SoDienThoaiNhan.Trim(),
            MaTinhThanh = input.MaTinhThanh,
            MaPhuongXa = input.MaPhuongXa,
            TenTinhThanh = tenTinh,
            TenPhuongXa = tenXa,
            DiaChiChiTiet = input.DiaChiChiTiet.Trim(),
            LaMacDinh = shouldBeDefault,
            NgayTao = DateTime.UtcNow
        };

        db.DiaChiNguoiDungs.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        if (shouldBeDefault)
            await SyncUserDefaultAddressAsync(userId, entity.DiaChiDayDu(), cancellationToken);

        return (true, null, entity);
    }

    public async Task<(bool Success, string? Error)> UpsertDefaultFromProfileAsync(
        int userId, UserAddressInput input, CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);

        var (okArea, areaErr, tenTinh, tenXa) = await ResolveAreaAsync(input.MaTinhThanh, input.MaPhuongXa, cancellationToken);
        if (!okArea) return (false, areaErr);

        var currentDefault = await db.DiaChiNguoiDungs
            .Where(a => a.MaNguoiDung == userId && a.LaMacDinh)
            .OrderByDescending(a => a.MaDiaChi)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentDefault is null)
        {
            currentDefault = new DiaChiNguoiDung
            {
                MaNguoiDung = userId,
                NhanDiaChi = string.IsNullOrWhiteSpace(input.NhanDiaChi) ? "Mặc định" : input.NhanDiaChi.Trim(),
                LaMacDinh = true,
                NgayTao = DateTime.UtcNow
            };
            db.DiaChiNguoiDungs.Add(currentDefault);
        }

        currentDefault.TenNguoiNhan = input.TenNguoiNhan.Trim();
        currentDefault.SoDienThoaiNhan = input.SoDienThoaiNhan.Trim();
        currentDefault.MaTinhThanh = input.MaTinhThanh;
        currentDefault.MaPhuongXa = input.MaPhuongXa;
        currentDefault.TenTinhThanh = tenTinh;
        currentDefault.TenPhuongXa = tenXa;
        currentDefault.DiaChiChiTiet = input.DiaChiChiTiet.Trim();

        await db.SaveChangesAsync(cancellationToken);
        await SyncUserDefaultAddressAsync(userId, currentDefault.DiaChiDayDu(), cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SetDefaultAsync(int userId, int maDiaChi, CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);

        var list = await db.DiaChiNguoiDungs
            .Where(a => a.MaNguoiDung == userId)
            .ToListAsync(cancellationToken);

        var target = list.FirstOrDefault(a => a.MaDiaChi == maDiaChi);
        if (target is null) return (false, "Không tìm thấy địa chỉ.");

        foreach (var item in list)
            item.LaMacDinh = item.MaDiaChi == maDiaChi;

        await db.SaveChangesAsync(cancellationToken);
        await SyncUserDefaultAddressAsync(userId, target.DiaChiDayDu(), cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int userId, int maDiaChi, CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);

        var target = await db.DiaChiNguoiDungs
            .FirstOrDefaultAsync(a => a.MaDiaChi == maDiaChi && a.MaNguoiDung == userId, cancellationToken);
        if (target is null) return (false, "Không tìm thấy địa chỉ.");

        var wasDefault = target.LaMacDinh;
        db.DiaChiNguoiDungs.Remove(target);
        await db.SaveChangesAsync(cancellationToken);

        if (wasDefault)
        {
            var next = await db.DiaChiNguoiDungs
                .Where(a => a.MaNguoiDung == userId)
                .OrderByDescending(a => a.MaDiaChi)
                .FirstOrDefaultAsync(cancellationToken);
            if (next is not null)
            {
                next.LaMacDinh = true;
                await db.SaveChangesAsync(cancellationToken);
                await SyncUserDefaultAddressAsync(userId, next.DiaChiDayDu(), cancellationToken);
            }
        }

        return (true, null);
    }

    private async Task<(bool Ok, string? Error, string TenTinh, string TenXa)> ResolveAreaAsync(
        int maTinh, int maXa, CancellationToken ct)
    {
        var hopLe = await khuVuc.PhuongXaThuocTinhAsync(maTinh, maXa, ct);
        if (!hopLe) return (false, "Phường/Xã không thuộc Tỉnh/Thành phố đã chọn.", "", "");

        var tinhs = await khuVuc.GetTinhThanhAsync(ct);
        var xas = await khuVuc.GetPhuongXaTheoTinhAsync(maTinh, ct);
        var tenTinh = tinhs.FirstOrDefault(t => t.Ma == maTinh)?.Ten ?? "";
        var tenXa = xas.FirstOrDefault(x => x.Ma == maXa)?.Ten ?? "";
        return (true, null, tenTinh, tenXa);
    }

    private async Task SyncUserDefaultAddressAsync(int userId, string fullAddress, CancellationToken ct)
    {
        var user = await db.NguoiDungs.FirstOrDefaultAsync(u => u.MaNguoiDung == userId, ct);
        if (user is null) return;
        user.DiaChiMacDinh = fullAddress.Length <= 500 ? fullAddress : fullAddress[..500];
        user.NgayCapNhat = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
