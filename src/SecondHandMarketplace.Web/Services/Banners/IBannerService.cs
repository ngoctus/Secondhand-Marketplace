using SecondHandMarketplace.Web.Models.Entities;

namespace SecondHandMarketplace.Web.Services.Banners;

/// <summary>Quản lý banner trang chủ (B5).</summary>
public interface IBannerService
{
    /// <summary>Banner đang hiển thị và trong hiệu lực, theo thứ tự ưu tiên.</summary>
    Task<IReadOnlyList<Banner>> LayBannerHienThiAsync(CancellationToken cancellationToken = default);

    /// <summary>Toàn bộ banner cho màn Admin.</summary>
    Task<IReadOnlyList<Banner>> LayTatCaAsync(CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, int Id)> SaveAsync(
        int? id, string tieuDe, string? moTa, string duongDanAnh, string? lienKet,
        int thuTu, bool dangHienThi, DateTime? batDauTu, DateTime? ketThucLuc,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
