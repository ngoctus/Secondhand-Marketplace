using SecondHandMarketplace.Web.Data;

namespace SecondHandMarketplace.Web.Services.Banners;

public class BannerService : IBannerService
{
    private readonly AppDbContext _db;

    public BannerService(AppDbContext db) => _db = db;
}
