using Web2Hand.Models.Enums;

namespace Web2Hand.Models.ViewModels.Products;

/// <summary>Bộ lọc tìm kiếm sản phẩm (lấy từ query string: /Products?Keyword=iphone&MaxPrice=5000000)</summary>
public class ProductSearchVm
{
    public string? Keyword { get; set; }          // tên sản phẩm hoặc tên người bán
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public ProductCondition? Condition { get; set; }
    public DeliveryMethod? DeliveryMethod { get; set; }
    public SaleType? SaleType { get; set; }
}
