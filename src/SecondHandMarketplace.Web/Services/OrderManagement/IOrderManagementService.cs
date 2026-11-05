using SecondHandMarketplace.Web.Models.ViewModels;
using SecondHandMarketplace.Web.Models.ViewModels.Orders;

namespace SecondHandMarketplace.Web.Services.OrderManagement;

public interface IOrderManagementService
{
    // E1: người bán
    Task<bool> HasShopAsync(int userId, CancellationToken ct = default);
    Task<PagedResult<OrderRowVm>> SearchSellerOrdersAsync(int userId, string? status, string? q, int page, int pageSize, CancellationToken ct = default);
    Task<Dictionary<string, int>> CountSellerOrdersByStatusAsync(int userId, CancellationToken ct = default);
    Task<OrderDetailVm?> GetSellerOrderAsync(int userId, int orderId, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> SellerUpdateStatusAsync(int userId, int orderId, string newStatus, string? reason, CancellationToken ct = default);

    // E2: Admin
    Task<PagedResult<OrderRowVm>> SearchAdminOrdersAsync(AdminOrderFilter filter, int pageSize, CancellationToken ct = default);
    Task<List<ShopOptionVm>> GetShopOptionsAsync(CancellationToken ct = default);
    Task<OrderDetailVm?> GetAdminOrderAsync(int orderId, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> AdminUpdateStatusAsync(int adminId, int orderId, string newStatus, string reason, CancellationToken ct = default);
}