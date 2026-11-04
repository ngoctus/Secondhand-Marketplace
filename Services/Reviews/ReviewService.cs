using SecondHandMarketplace.Web.Data;

namespace SecondHandMarketplace.Web.Services.Reviews;

public class ReviewService : IReviewService
{
    private readonly AppDbContext _db;

    public ReviewService(AppDbContext db) => _db = db;
}
