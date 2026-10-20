using SecondHandMarketplace.Web.Data;

namespace SecondHandMarketplace.Web.Services.Disputes;

public class DisputeService : IDisputeService
{
    private readonly AppDbContext _db;

    public DisputeService(AppDbContext db) => _db = db;
}
