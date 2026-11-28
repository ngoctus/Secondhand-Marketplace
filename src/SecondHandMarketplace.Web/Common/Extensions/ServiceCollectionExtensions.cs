using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Services.Auctions;
using SecondHandMarketplace.Web.Services.OrderManagement;
using SecondHandMarketplace.Web.Services.Ai;
using SecondHandMarketplace.Web.Services.Auth;
using SecondHandMarketplace.Web.Services.AuditLogs;
using SecondHandMarketplace.Web.Services.Banners;
using SecondHandMarketplace.Web.Services.Carts;
using SecondHandMarketplace.Web.Services.Categories;
using SecondHandMarketplace.Web.Services.Dashboard;
using SecondHandMarketplace.Web.Services.Disputes;
using SecondHandMarketplace.Web.Services.Favorites;
using SecondHandMarketplace.Web.Services.Moderation;
using SecondHandMarketplace.Web.Services.ProductReview;
using SecondHandMarketplace.Web.Services.Notifications;
using SecondHandMarketplace.Web.Services.Orders;
using SecondHandMarketplace.Web.Services.Products;
using SecondHandMarketplace.Web.Services.Reports;
using SecondHandMarketplace.Web.Services.Reviews;
using SecondHandMarketplace.Web.Services.Accounts;
using SecondHandMarketplace.Web.Services.Shops;
using SecondHandMarketplace.Web.Services.Realtime;
using SecondHandMarketplace.Web.Services.Chat;
using SecondHandMarketplace.Web.Services.Areas;

namespace SecondHandMarketplace.Web.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        var databaseServer = config["Database:Server"];
        var hasStructuredSettings = !string.IsNullOrWhiteSpace(databaseServer)
            || !string.IsNullOrWhiteSpace(config["Database:User"])
            || !string.IsNullOrWhiteSpace(config["Database:Password"]);
        var connectionString = config.GetConnectionString("DefaultConnection");

        if (hasStructuredSettings)
        {
            var databaseUser = config["Database:User"];
            var databasePassword = config["Database:Password"];
            if (string.IsNullOrWhiteSpace(databaseServer)
                || string.IsNullOrWhiteSpace(databaseUser)
                || string.IsNullOrWhiteSpace(databasePassword))
            {
                throw new InvalidOperationException("Database:Server, Database:User, and Database:Password must all be configured together.");
            }

            connectionString = new SqlConnectionStringBuilder
            {
                DataSource = databaseServer,
                InitialCatalog = config["Database:Name"] ?? "SecondHandMarketplace",
                UserID = databaseUser,
                Password = databasePassword,
                Encrypt = true,
                TrustServerCertificate = config.GetValue("Database:TrustServerCertificate", false),
                MultipleActiveResultSets = true
            }.ConnectionString;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("A SQL Server connection string or complete Database configuration is required.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));
        return services;
    }

    /// <summary>
    /// ThÃªm service má»›i? ÄÄƒng kÃ½ á»Ÿ Ä‘Ã¢y (1 dÃ²ng). Lá»—i "Unable to resolve service..." = quÃªn dÃ²ng nÃ y.
    /// </summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IRegistrationOtpService, RegistrationOtpService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        // Khiếu nại & Mở khóa tài khoản:
        services.AddScoped<IAccountAppealAdminService, AccountAppealAdminService>();
        services.AddScoped<IAccountAppealUserService, AccountAppealUserService>();
        services.AddScoped<IUserAddressService, UserAddressService>();
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<IKhuVucService, KhuVucService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddAiChat();   // C3: IAiChatService (typed HttpClient), IAiSettingsService, IAiRequestThrottle
        services.AddScoped<IKeywordScanner, SqlKeywordScanner>();
        services.AddHttpClient<IAiModerationAnalyzer, AiModerationAnalyzer>()
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddScoped<IModerationService, ModerationService>();
        services.AddScoped<IForbiddenKeywordService, ForbiddenKeywordService>();
        services.AddScoped<IProductReviewService, ProductReviewService>();

        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IDisputeService, DisputeService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBannerService, BannerService>();
        services.AddScoped<IDashboardService, DashboardService>();
        // Pháº§n E
        services.AddScoped<IOrderManagementService, OrderManagementService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IAuctionRealtime, AuctionRealtime>();
        services.AddScoped<IAuctionService, AuctionService>();
        services.AddHostedService<AuctionBackgroundService>();
        services.AddHostedService<OrderCleanupBackgroundService>();

        return services;
    }
}
