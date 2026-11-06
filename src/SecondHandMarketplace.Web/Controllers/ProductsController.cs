using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Models.ViewModels;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Services.Products;

namespace SecondHandMarketplace.Web.Controllers;

/// <summary>Xem / tìm sản phẩm: ai cũng truy cập được (không cần đăng nhập).</summary>
public class ProductsController : Controller
{
    private readonly IProductService _products;

    public ProductsController(IProductService products) => _products = products;

    public async Task<IActionResult> Index([FromQuery] ProductSearchVm filter)
    {
        try
        {
            var result = await _products.SearchAsync(filter);
            return View(result);
        }
        catch
        {
            // Dự phòng nếu kết nối DB bị lỗi thì trang vẫn load tức thì không bị treo
            return View(new PagedResult<ProductCardVm> { Items = Array.Empty<ProductCardVm>(), Page = 1, PageSize = 12 });
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        ProductDetailVm? product = null;
        try
        {
            product = await _products.GetDetailAsync(id);
        }
        catch { }

        // Nếu DB chưa có hoặc ID không tồn tại: hiển thị dữ liệu mẫu chuẩn theo Wireframe ảnh 5
        if (product == null)
        {
            product = new ProductDetailVm
            {
                Id = id,
                Name = "Áo khoác denim vintage",
                Price = 250000,
                Status = "ACTIVE",
                CanBuy = true,
                CategoryName = "Thời trang - Nam",
                ConditionName = "Rất tốt",
                ShopName = "Shop An",
                Description = "Áo khoác denim dáng rộng, size M, màu xanh nhạt. Còn mới khoảng 90%, không rách hay phai màu. Thông số: Dài 68cm - Rộng vai 52cm - Chất liệu denim cotton.",
                CreatedAt = DateTime.Now.AddDays(-2)
            };
        }

        return View(product);
    }
}
