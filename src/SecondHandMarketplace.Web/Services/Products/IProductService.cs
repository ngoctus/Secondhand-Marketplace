using SecondHandMarketplace.Web.Models.ViewModels.Products;

namespace SecondHandMarketplace.Web.Services.Products;

public interface IProductService
{
    /// <summary>Tìm sản phẩm công khai (ACTIVE, chưa xóa, shop và chủ shop đang ACTIVE).</summary>
    Task<List<ProductCardVm>> SearchAsync(ProductSearchVm filter);

    /// <summary>Chi tiết sản phẩm công khai (ACTIVE / RESERVED / SOLD). Không có hoặc không được xem thì trả về null.</summary>
    Task<ProductDetailVm?> GetDetailAsync(int id);

    // TODO (User đăng bán): CreateAsync, UpdateAsync, HideAsync
    // TODO (Admin): ApproveAsync, RejectAsync
}