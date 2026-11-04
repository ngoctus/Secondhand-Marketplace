using Microsoft.EntityFrameworkCore;

namespace SecondHandMarketplace.Web.Models.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Precision(18, 2)]
    public decimal Price { get; set; }   // giá tại thời điểm đặt
}
