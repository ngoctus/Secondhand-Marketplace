using Web2Hand.Data;

namespace Web2Hand.Services.Reviews;

public class ReviewService : IReviewService
{
    private readonly AppDbContext _db;

    public ReviewService(AppDbContext db) => _db = db;
}
