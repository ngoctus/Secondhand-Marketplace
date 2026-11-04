using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Services.Ai;
using SecondHandMarketplace.Web.Services.AuditLogs;
using SecondHandMarketplace.Web.Services.Banners;
using SecondHandMarketplace.Web.Services.Carts;
using SecondHandMarketplace.Web.Services.Categories;
using SecondHandMarketplace.Web.Services.Dashboard;
using SecondHandMarketplace.Web.Services.Disputes;
using SecondHandMarketplace.Web.Services.Favorites;
using SecondHandMarketplace.Web.Services.Orders;
using SecondHandMarketplace.Web.Services.Products;
using SecondHandMarketplace.Web.Services.Reports;
using SecondHandMarketplace.Web.Services.Reviews;

namespace SecondHandMarketplace.Web.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
        return services;
    }

    /// <summary>
    /// Thêm service mới? Đăng ký ở đây (1 dòng). Lỗi "Unable to resolve service..." = quên dòng này.
    /// </summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAiChatService, AiChatService>();

        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IDisputeService, DisputeService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBannerService, BannerService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
