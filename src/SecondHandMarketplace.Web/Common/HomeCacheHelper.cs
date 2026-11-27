using Microsoft.Extensions.Caching.Distributed;

namespace SecondHandMarketplace.Web.Common;

/// <summary>
/// Trang chủ dựng một bản cache dùng chung cho mọi khách. Mọi luồng làm thay đổi dữ liệu hiển thị trên
/// trang chủ phải gọi helper này để trang chủ cập nhật ngay thay vì chờ hết hạn cache (quan trọng khi demo).
/// </summary>
public static class HomeCacheHelper
{
    public const string CatalogCacheKey = "home:catalog";

    public static TimeSpan CacheTrangChu { get; } = TimeSpan.FromMinutes(5);

    public static async Task XoaCacheTrangChuAsync(
        IDistributedCache? distributedCache,
        CancellationToken cancellationToken = default)
    {
        if (distributedCache is not null)
        {
            try
            {
                await distributedCache.RemoveAsync(CatalogCacheKey, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Cache is an optimization. A cache outage must not turn a committed order or
                // moderation action into an apparent failure; the entry naturally expires by TTL.
            }
        }
    }
}
