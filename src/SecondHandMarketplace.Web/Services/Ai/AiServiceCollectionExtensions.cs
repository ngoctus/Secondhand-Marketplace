using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SecondHandMarketplace.Web.Services.Ai;

public static class AiServiceCollectionExtensions
{
    /// <summary>
    /// Đăng ký toàn bộ dịch vụ C3 (Chatbox AI). Được gọi từ ServiceCollectionExtensions.AddAppServices
    /// thay cho đăng ký cũ AddScoped&lt;IAiChatService, AiChatService&gt;(), nên KHÔNG đăng ký IAiChatService ở nơi khác.
    ///  - AiChatService: typed HttpClient (transient theo cơ chế của AddHttpClient), nhận IAiSettingsService qua DI.
    ///    HttpClient.Timeout đặt vô hạn vì thời hạn do AiChatService tự kiểm soát bằng CancellationToken (Ai:TimeoutSeconds).
    ///  - IAiSettingsService: scoped (dùng AppDbContext).
    ///  - IAiRequestThrottle: singleton (phải giữ trạng thái giới hạn giữa các request).
    /// </summary>
    public static IServiceCollection AddAiChat(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IAiRequestThrottle, AiRequestThrottle>();
        services.AddScoped<IAiSettingsService, AiSettingsService>();
        services.AddHttpClient<IAiChatService, AiChatService>()
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan);
        return services;
    }
}
