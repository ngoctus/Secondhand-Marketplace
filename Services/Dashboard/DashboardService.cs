using SecondHandMarketplace.Web.Data;

namespace SecondHandMarketplace.Web.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db) => _db = db;
}
