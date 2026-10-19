using Web2Hand.Services.Auctions;

namespace Web2Hand.BackgroundJobs;

/// <summary>Chạy nền, mỗi 30 giây gọi AuctionService.RefreshStatusesAsync (mở / đóng phiên đấu giá).</summary>
public class AuctionJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionJob> _logger;

    public AuctionJob(IServiceScopeFactory scopeFactory, ILogger<AuctionJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var auctions = scope.ServiceProvider.GetRequiredService<IAuctionService>();
                await auctions.RefreshStatusesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái phiên đấu giá");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
