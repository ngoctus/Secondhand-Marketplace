using Web2Hand.Data;

namespace Web2Hand.Services.Banners;

public class BannerService : IBannerService
{
    private readonly AppDbContext _db;

    public BannerService(AppDbContext db) => _db = db;
}
