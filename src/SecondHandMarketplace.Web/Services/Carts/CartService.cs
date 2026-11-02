using SecondHandMarketplace.Web.Data;

namespace SecondHandMarketplace.Web.Services.Carts;

public class CartService : ICartService
{
    private readonly AppDbContext _db;

    public CartService(AppDbContext db) => _db = db;
}
