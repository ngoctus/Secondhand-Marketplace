using Web2Hand.Data;

namespace Web2Hand.Services.Carts;

public class CartService : ICartService
{
    private readonly AppDbContext _db;

    public CartService(AppDbContext db) => _db = db;
}
