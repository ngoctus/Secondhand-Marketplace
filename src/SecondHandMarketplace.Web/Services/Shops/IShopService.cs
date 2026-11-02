using SecondHandMarketplace.Web.Models.Entities;

namespace SecondHandMarketplace.Web.Services.Shops;

public interface IShopService
{
    Task<CuaHang?> GetByOwnerAsync(int userId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> SaveAsync(int userId, string name, string phone, string? description, string? logo, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> LockShopAsync(int? actorId, int shopId, string? reason, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UnlockShopAsync(int? actorId, int shopId, CancellationToken cancellationToken = default);
}