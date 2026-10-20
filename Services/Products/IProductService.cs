using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Products;

namespace SecondHandMarketplace.Web.Services.Products;

public interface IProductService
{
    Task<List<ProductCardVm>> SearchAsync(ProductSearchVm filter);
    Task<Product?> GetDetailAsync(int id);

    // TODO (User đăng bán): CreateAsync, UpdateAsync, HideAsync
    // TODO (Admin): ApproveAsync, RejectAsync
}
