using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Models.ViewModels;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Services.Categories;
using SecondHandMarketplace.Web.Services.Products;

namespace SecondHandMarketplace.Web.Controllers;

/// <summary>Xem / tìm sản phẩm: ai cũng truy cập được (không cần đăng nhập).</summary>
public class ProductsController : Controller
{
    private readonly IProductService _products;
    private readonly ICategoryService _categories;
    private readonly IShopDisplayReader _hienThiShop;

    public ProductsController(IProductService products, ICategoryService categories, IShopDisplayReader hienThiShop)
    {
        _products = products;
        _categories = categories;
        _hienThiShop = hienThiShop;
    }

    public async Task<IActionResult> Index([FromQuery] ProductSearchVm filter)
    {
        var ketQua = await _products.SearchPagedAsync(filter);

        return View(new ProductSearchResultVm
        {
            Filter = filter,
            Result = ketQua,
            DanhSachDanhMuc = await _categories.GetCategoriesAsync(),
            DanhSachTinhTrang = await _categories.GetConditionsAsync(),
            KhuVucDaChon = await _hienThiShop.LayTenKhuVucAsync(filter.MaTinhThanh, filter.MaPhuongXa)
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _products.GetDetailAsync(id);

        // Không còn sản phẩm mẫu dựng sẵn: tin không tồn tại, đã bị ẩn hoặc đã xóa mềm phải trả 404 thật.
        if (product is null) return NotFound();

        return View(product);
    }
}
