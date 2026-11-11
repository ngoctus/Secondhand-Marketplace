using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Common;

namespace SecondHandMarketplace.Web.Models.ViewModels.Shop;

/// <summary>
/// Dữ liệu trang shop công khai. Cố ý KHÔNG có số điện thoại, địa chỉ chi tiết, thông tin ngân hàng, mã chủ shop.
/// </summary>
public sealed class ShopPublicVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? Description { get; set; }
    public DateTime JoinedAtUtc { get; set; }

    /// <summary>Tên xã/phường (có thể null với shop cũ chưa bổ sung địa chỉ).</summary>
    public string? WardName { get; set; }
    public string? ProvinceName { get; set; }

    public string? AreaText => string.Join(", ", new[] { WardName, ProvinceName }.Where(x => !string.IsNullOrWhiteSpace(x)));

    /// <summary>Điểm trung bình; null = chưa có dữ liệu từ vw_DiemCuaHang hoặc chưa có đánh giá.</summary>
    public decimal? AverageRating { get; set; }
    public int? ReviewCount { get; set; }
    public int? CompletedOrders { get; set; }

    public PagedResult<ProductCardVm> Products { get; set; } = PagedResult<ProductCardVm>.Empty(1, 12);
}
