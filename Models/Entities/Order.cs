using Microsoft.EntityFrameworkCore;
using Web2Hand.Models.Enums;

namespace Web2Hand.Models.Entities;

/// <summary>Mỗi đơn thuộc 1 shop. Giỏ nhiều shop sẽ tách thành nhiều đơn.</summary>
public class Order
{
    public int Id { get; set; }

    public string BuyerId { get; set; } = string.Empty;
    public AppUser Buyer { get; set; } = null!;

    public string SellerId { get; set; } = string.Empty;
    public AppUser Seller { get; set; } = null!;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; }

    // Thông tin nhận hàng
    public string ReceiverName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? ReceiveTimeNote { get; set; }      // VD: "né giờ hành chính"

    [Precision(18, 2)]
    public decimal TotalAmount { get; set; }

    public string? CancelReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = new();
}
