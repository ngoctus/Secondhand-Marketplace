using SecondHandMarketplace.Web.Data;

namespace SecondHandMarketplace.Web.Services.Orders;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db) => _db = db;
}
