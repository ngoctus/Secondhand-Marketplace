using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.Enums;
using SecondHandMarketplace.Web.Models.ViewModels.Products;

namespace SecondHandMarketplace.Web.Services.Products;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db) => _db = db;

    public async Task<List<ProductCardVm>> SearchAsync(ProductSearchVm f)
    {
        // Chỉ hiện sản phẩm đã được Admin duyệt và chưa có đơn
        var query = _db.Products.AsNoTracking().Where(p => p.Status == ProductStatus.Active);

        if (!string.IsNullOrWhiteSpace(f.Keyword))
            query = query.Where(p => p.Name.Contains(f.Keyword) || p.Seller.FullName.Contains(f.Keyword));
        if (f.CategoryId != null)
            query = query.Where(p => p.CategoryId == f.CategoryId);
        if (f.MinPrice != null)
            query = query.Where(p => p.Price >= f.MinPrice);
        if (f.MaxPrice != null)
            query = query.Where(p => p.Price <= f.MaxPrice);
        if (f.Condition != null)
            query = query.Where(p => p.Condition == f.Condition);
        if (f.DeliveryMethod != null)
            query = query.Where(p => p.DeliveryMethod == f.DeliveryMethod);
        if (f.SaleType != null)
            query = query.Where(p => p.SaleType == f.SaleType);

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductCardVm
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                ImageUrl = p.Images.Select(i => i.Url).FirstOrDefault(),
                SellerName = p.Seller.FullName,
                Condition = p.Condition,
                SaleType = p.SaleType
            })
            .ToListAsync();
    }

    public async Task<Product?> GetDetailAsync(int id)
    {
        return await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Seller)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}
