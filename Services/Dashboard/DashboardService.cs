using Web2Hand.Data;

namespace Web2Hand.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db) => _db = db;
}
