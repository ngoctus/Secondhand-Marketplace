namespace Web2Hand.Models.Entities;

/// <summary>Buyer đánh giá Seller sau khi đơn COMPLETED.</summary>
public class Review
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string BuyerId { get; set; } = string.Empty;
    public AppUser Buyer { get; set; } = null!;
    public string SellerId { get; set; } = string.Empty;
    public AppUser Seller { get; set; } = null!;
    public int Stars { get; set; }           // 1..5
    public string Comment { get; set; } = string.Empty;
    public bool IsHidden { get; set; }       // Admin ẩn đánh giá spam / vi phạm
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
