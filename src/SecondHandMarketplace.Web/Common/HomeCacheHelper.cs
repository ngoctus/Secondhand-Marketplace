using Microsoft.Extensions.Caching.Memory;

namespace SecondHandMarketplace.Web.Common;

/// <summary>
/// Trang chủ dựng một bản cache dùng chung cho mọi khách. Mọi luồng làm thay đổi dữ liệu hiển thị trên
/// trang chủ phải gọi helper này để trang chủ cập nhật ngay thay vì chờ hết hạn cache (quan trọng khi demo).
/// </summary>
public static class HomeCacheHelper
{
    public const string CatalogCacheKey = "home:catalog";

    public static TimeSpan CacheTrangChu { get; } = TimeSpan.FromMinutes(5);

    public static void XoaCacheTrangChu(IMemoryCache? cache) => cache?.Remove(CatalogCacheKey);
}
