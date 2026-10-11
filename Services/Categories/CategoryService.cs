using Web2Hand.Data;

namespace Web2Hand.Services.Categories;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db) => _db = db;
}
