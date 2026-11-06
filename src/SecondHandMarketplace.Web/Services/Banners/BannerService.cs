using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;

namespace SecondHandMarketplace.Web.Services.Banners;

public class BannerService : IBannerService
{
    private readonly AppDbContext _db;

    public BannerService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Banner>> LayBannerHienThiAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _db.Banners
            .AsNoTracking()
            .Where(b => b.DangHienThi
                        && (b.BatDauTu == null || b.BatDauTu <= now)
                        && (b.KetThucLuc == null || b.KetThucLuc > now))
            .OrderBy(b => b.ThuTuHienThi)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Banner>> LayTatCaAsync(CancellationToken cancellationToken = default) =>
        await _db.Banners.AsNoTracking().OrderBy(b => b.ThuTuHienThi).ToListAsync(cancellationToken);

    public async Task<(bool Success, string? Error, int Id)> SaveAsync(
        int? id, string tieuDe, string? moTa, string duongDanAnh, string? lienKet,
        int thuTu, bool dangHienThi, DateTime? batDauTu, DateTime? ketThucLuc,
        CancellationToken cancellationToken = default)
    {
        tieuDe = tieuDe?.Trim() ?? string.Empty;
        duongDanAnh = duongDanAnh?.Trim() ?? string.Empty;

        if (tieuDe.Length is < 3 or > 200) return (false, "Tiêu đề phải từ 3 đến 200 ký tự.", 0);
        if (string.IsNullOrWhiteSpace(duongDanAnh)) return (false, "Đường dẫn ảnh là bắt buộc.", 0);
        if (duongDanAnh.Length > 500) return (false, "Đường dẫn ảnh tối đa 500 ký tự.", 0);
        if (moTa != null && moTa.Length > 500) return (false, "Mô tả tối đa 500 ký tự.", 0);
        if (thuTu < 1) return (false, "Thứ tự hiển thị phải từ 1 trở lên.", 0);
        if (batDauTu != null && ketThucLuc != null && ketThucLuc <= batDauTu)
            return (false, "Thời gian kết thúc phải sau thời gian bắt đầu.", 0);

        Banner banner;
        if (id is null)
        {
            banner = new Banner { NgayTao = DateTime.UtcNow };
            _db.Banners.Add(banner);
        }
        else
        {
            var existing = await _db.Banners.FirstOrDefaultAsync(b => b.MaBanner == id, cancellationToken);
            if (existing is null) return (false, "Không tìm thấy banner.", 0);
            banner = existing;
        }

        banner.TieuDe = tieuDe;
        banner.MoTa = string.IsNullOrWhiteSpace(moTa) ? null : moTa.Trim();
        banner.DuongDanAnh = duongDanAnh;
        banner.LienKet = string.IsNullOrWhiteSpace(lienKet) ? null : lienKet.Trim();
        banner.ThuTuHienThi = thuTu;
        banner.DangHienThi = dangHienThi;
        banner.BatDauTu = batDauTu;
        banner.KetThucLuc = ketThucLuc;
        banner.NgayCapNhat = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return (false, "Không lưu được banner, vui lòng kiểm tra lại thông tin.", 0);
        }
        return (true, null, banner.MaBanner);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var banner = await _db.Banners.FirstOrDefaultAsync(b => b.MaBanner == id, cancellationToken);
        if (banner is null) return (false, "Không tìm thấy banner.");
        _db.Banners.Remove(banner);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, "Đã xóa banner.");
    }
}
