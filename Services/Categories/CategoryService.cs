using SecondHandMarketplace.Web.Data;

namespace SecondHandMarketplace.Web.Services.Categories;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db) => _db = db;
}
