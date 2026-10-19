using Microsoft.AspNetCore.Mvc;
using Web2Hand.Models.Enums;
using Web2Hand.Models.ViewModels.Products;
using Web2Hand.Services.Products;

namespace Web2Hand.Controllers;

/// <summary>Xem / tìm sản phẩm: ai cũng truy cập được (không cần đăng nhập).</summary>
public class ProductsController : Controller
{
    private readonly IProductService _products;

    public ProductsController(IProductService products) => _products = products;

    public async Task<IActionResult> Index([FromQuery] ProductSearchVm filter)
        => View(await _products.SearchAsync(filter));

    public async Task<IActionResult> Details(int id)
    {
        var product = await _products.GetDetailAsync(id);
        if (product == null || product.Status == ProductStatus.PendingReview || product.Status == ProductStatus.Rejected)
            return NotFound();
        return View(product);
    }
}
