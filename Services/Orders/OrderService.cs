using Web2Hand.Data;

namespace Web2Hand.Services.Orders;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db) => _db = db;
}
