using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Shop;

namespace SecondHandMarketplace.Web.Services.Shops;

/// <summary>Dữ liệu tạo/cập nhật shop (đã tách khỏi ViewModel để dùng chung cho MVC và API).</summary>
public sealed record ShopSaveInput(
    string Name,
    string Phone,
    string? Description,
    string? Logo,
    int? MaTinhThanh,
    int? MaPhuongXa,
    string? DiaChiChiTiet,
    string? KhuVucHienThi,
    string? MaNganHang,
    string? SoTaiKhoan,
    string? TenChuTaiKhoan);

public interface IShopService
{
    Task<CuaHang?> GetByOwnerAsync(int userId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> SaveAsync(int userId, ShopSaveInput input, CancellationToken cancellationToken = default);

    /// <summary>Trang shop công khai; null nếu shop không tồn tại, bị khóa hoặc chủ shop không còn hoạt động (controller trả 404).</summary>
    Task<ShopPublicVm?> GetPublicShopAsync(int id, int page = 1, int pageSize = 12, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> LockShopAsync(int? actorId, int shopId, string? reason, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UnlockShopAsync(int? actorId, int shopId, CancellationToken cancellationToken = default);
}
