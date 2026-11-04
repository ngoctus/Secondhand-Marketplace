using SecondHandMarketplace.Web.Models.Enums;

namespace SecondHandMarketplace.Web.Models.ViewModels.Products;

/// <summary>Dữ liệu hiển thị 1 sản phẩm trong danh sách.</summary>
public class ProductCardVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public ProductCondition Condition { get; set; }
    public SaleType SaleType { get; set; }
}
