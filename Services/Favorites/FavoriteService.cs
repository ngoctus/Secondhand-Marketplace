using Web2Hand.Data;

namespace Web2Hand.Services.Favorites;

public class FavoriteService : IFavoriteService
{
    private readonly AppDbContext _db;

    public FavoriteService(AppDbContext db) => _db = db;
}
