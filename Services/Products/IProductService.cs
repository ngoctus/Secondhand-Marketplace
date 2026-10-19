using Web2Hand.Models.Entities;
using Web2Hand.Models.ViewModels.Products;

namespace Web2Hand.Services.Products;

public interface IProductService
{
    Task<List<ProductCardVm>> SearchAsync(ProductSearchVm filter);
    Task<Product?> GetDetailAsync(int id);

    // TODO (Seller): CreateAsync, UpdateAsync, HideAsync
    // TODO (Admin): ApproveAsync, RejectAsync
}
