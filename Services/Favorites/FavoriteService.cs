using SecondHandMarketplace.Web.Data;

namespace SecondHandMarketplace.Web.Services.Favorites;

public class FavoriteService : IFavoriteService
{
    private readonly AppDbContext _db;

    public FavoriteService(AppDbContext db) => _db = db;
}
