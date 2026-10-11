using Web2Hand.Data;

namespace Web2Hand.Services.Disputes;

public class DisputeService : IDisputeService
{
    private readonly AppDbContext _db;

    public DisputeService(AppDbContext db) => _db = db;
}
