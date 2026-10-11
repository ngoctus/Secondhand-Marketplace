using Microsoft.EntityFrameworkCore;
using Web2Hand.Models.Enums;

namespace Web2Hand.Models.Entities;

public class Product
{
    public int Id { get; set; }

    public string SellerId { get; set; } = string.Empty;
    public AppUser Seller { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Giá bán ngay (BuyNow) hoặc giá khởi điểm (Auction).</summary>
    [Precision(18, 2)]
    public decimal Price { get; set; }

    public ProductCondition Condition { get; set; }
    public SaleType SaleType { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.PendingReview;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ProductImage> Images { get; set; } = new();
}
