using SecondHandMarketplace.Web.Models.ViewModels.Dashboard;

namespace SecondHandMarketplace.Web.Services.Dashboard;

/// <summary>Thống kê cho Admin và cho người bán.</summary>
public interface IDashboardService
{
    /// <summary>Trả null nếu người dùng chưa có cửa hàng.</summary>
    Task<SellerDashboardVm?> GetSellerAsync(int userId, CancellationToken ct = default);
    Task<ChartDataVm> GetSellerChartAsync(int userId, string mode, CancellationToken ct = default);
    Task<AdminDashboardVm> GetAdminAsync(CancellationToken ct = default);
    Task<ChartDataVm> GetAdminChartAsync(string mode, CancellationToken ct = default);
}