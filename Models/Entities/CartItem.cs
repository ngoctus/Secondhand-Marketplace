namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Mỗi sản phẩm chỉ có 1 cái nên không cần số lượng.</summary>
public class CartItem
{
    public int Id { get; set; }
    public string BuyerId { get; set; } = string.Empty;
    public AppUser Buyer { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
