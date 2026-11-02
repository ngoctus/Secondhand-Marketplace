namespace SecondHandMarketplace.Web.Models.ViewModels.Products;

/// <summary>Dữ liệu trang chi tiết sản phẩm.</summary>
public class ProductDetailVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;   // ACTIVE | RESERVED | SOLD
    public bool CanBuy { get; set; }
    public string? CategoryName { get; set; }
    public string? ConditionName { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string? ShopDescription { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Images { get; set; } = new();
}