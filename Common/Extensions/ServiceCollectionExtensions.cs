using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web2Hand.BackgroundJobs;
using Web2Hand.Data;
using Web2Hand.Models.Entities;
using Web2Hand.Services.Ai;
using Web2Hand.Services.AuditLogs;
using Web2Hand.Services.Auctions;
using Web2Hand.Services.Banners;
using Web2Hand.Services.Carts;
using Web2Hand.Services.Categories;
using Web2Hand.Services.Dashboard;
using Web2Hand.Services.Disputes;
using Web2Hand.Services.Favorites;
using Web2Hand.Services.Notifications;
using Web2Hand.Services.Orders;
using Web2Hand.Services.Products;
using Web2Hand.Services.Reports;
using Web2Hand.Services.Reviews;
using Web2Hand.Services.Users;

namespace Web2Hand.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
        return services;
    }

    public static IServiceCollection AddIdentityAndAuth(this IServiceCollection services)
    {
        services.AddIdentity<AppUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
        });
        return services;
    }

    /// <summary>
    /// Thêm service mới? Đăng ký ở đây (1 dòng). Lỗi "Unable to resolve service..." = quên dòng này.
    /// </summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IAuctionService, AuctionService>();
        services.AddScoped<INotificationService, NotificationService>();
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
        services.AddScoped<IUserAdminService, UserAdminService>();

        services.AddHostedService<AuctionJob>();
        return services;
    }
}
