using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Models.ViewModels;
using SecondHandMarketplace.Web.Models.ViewModels.Areas;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Services.Areas;
using SecondHandMarketplace.Web.Services.Categories;
using SecondHandMarketplace.Web.Services.Products;

namespace SecondHandMarketplace.Web.Controllers;

/// <summary>Xem / tìm sản phẩm: ai cũng truy cập được (không cần đăng nhập).</summary>
public class ProductsController : Controller
{
    private readonly IProductService _products;
    private readonly ICategoryService _categories;
    private readonly IKhuVucService _khuVuc;

    public ProductsController(IProductService products, ICategoryService categories, IKhuVucService khuVuc)
    {
        _products = products;
        _categories = categories;
        _khuVuc = khuVuc;
    }

    public async Task<IActionResult> Index([FromQuery] ProductSearchVm filter, CancellationToken cancellationToken)
    {
        var ketQua = await _products.SearchAsync(filter);

        var danhSachTinh = await _khuVuc.GetTinhThanhAsync(cancellationToken);
        var danhSachXa = new List<PhuongXaItem>();
        if (filter.MaTinhThanh is int maTinh)
            danhSachXa = (await _khuVuc.GetPhuongXaTheoTinhAsync(maTinh, cancellationToken)).ToList();

        var tenTinh = danhSachTinh.FirstOrDefault(t => t.Ma == filter.MaTinhThanh)?.Ten;
        var tenXa = danhSachXa.FirstOrDefault(x => x.Ma == filter.MaPhuongXa)?.Ten;

        return View(new ProductSearchResultVm
        {
            Filter = filter,
            Result = ketQua,
            DanhSachDanhMuc = await _categories.GetCategoriesAsync(cancellationToken),
            DanhSachTinhTrang = await _categories.GetConditionsAsync(cancellationToken),
            KhuVuc = new ChonKhuVucVm
            {
                MaTinh = filter.MaTinhThanh,
                MaXa = filter.MaPhuongXa,
                TinhThanhs = danhSachTinh,
                PhuongXas = danhSachXa,
                ChoPhepTatCa = true
            },
            KhuVucDaChon = (tenXa, tenTinh) switch
            {
                (not null, not null) => $"{tenXa}, {tenTinh}",
                (not null, _) => tenXa,
                (_, not null) => tenTinh,
                _ => null
            }
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
