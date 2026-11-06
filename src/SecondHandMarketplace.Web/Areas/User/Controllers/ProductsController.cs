using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Services.Categories;
using SecondHandMarketplace.Web.Services.Moderation;
using SecondHandMarketplace.Web.Services.Products;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

/// <summary>B1 – Người bán: đăng bán, sửa, ẩn, gửi duyệt sản phẩm (kèm ảnh).</summary>
public class ProductsController : UserBaseController
{
    private static readonly string[] DinhDangAnhChoPhep = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long MaxAnhBytes = 5 * 1024 * 1024;
    private const int SoAnhToiDa = 10;

    private readonly IProductService _products;
    private readonly ICategoryService _categories;
    private readonly IModerationService _moderation;
    private readonly IWebHostEnvironment _env;

    public ProductsController(IProductService products, ICategoryService categories, IModerationService moderation, IWebHostEnvironment env)
    {
        _products = products;
        _categories = categories;
        _moderation = moderation;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var userId = User.LayNguoiDungId()!.Value;
        return View(await _products.GetMyAsync(userId, page));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var form = new ProductFormVm();
        await DienDanhMucAsync(form);
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormVm form, List<IFormFile>? AnhMoi, bool? GuiDuyet)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var (anh, loiLuu) = await LuuAnhLenDiaAsync(AnhMoi);
        if (loiLuu is not null) ModelState.AddModelError(string.Empty, loiLuu);

        if (!ModelState.IsValid)
        {
            await DienDanhMucAsync(form);
            return View(form);
        }

        var (ok, loi, maSanPham) = await _products.CreateAsync(userId, form, anh);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, loi!);
            await DienDanhMucAsync(form);
            return View(form);
        }

        if (GuiDuyet == true) return await GuiDuyetVaChuyenVeIndexAsync(userId, maSanPham);

        this.ThongBaoThanhCong("Đã lưu bản nháp. Vào \"Gửi duyệt\" khi bạn sẵn sàng đăng bán.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var form = await _products.GetFormAsync(userId, id);
        if (form is null) return NotFound();
        await DienDanhMucAsync(form);
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductFormVm form, List<IFormFile>? AnhMoi, bool? GuiDuyet)
    {
        var userId = User.LayNguoiDungId()!.Value;
        if (form.Id is null || form.Id != id) form.Id = id;

        var (anh, loiLuu) = await LuuAnhLenDiaAsync(AnhMoi);
        if (loiLuu is not null) ModelState.AddModelError(string.Empty, loiLuu);

        if (!ModelState.IsValid)
        {
            form.AnhHienTai = (await _products.GetFormAsync(userId, id))?.AnhHienTai ?? new();
            await DienDanhMucAsync(form);
            return View(form);
        }

        var (ok, loi) = await _products.UpdateAsync(userId, id, form, anh);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, loi!);
            await DienDanhMucAsync(form);
            return View(form);
        }

        if (GuiDuyet == true) return await GuiDuyetVaChuyenVeIndexAsync(userId, id);

        this.ThongBaoThanhCong("Đã cập nhật sản phẩm.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuiDuyet(int id)
    {
        var userId = User.LayNguoiDungId()!.Value;
        return await GuiDuyetVaChuyenVeIndexAsync(userId, id);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> An(int id)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var (ok, loi) = await _products.HideAsync(userId, id);
        if (ok) this.ThongBaoThanhCong("Đã ẩn sản phẩm.");
        else this.ThongBaoLoi(loi ?? "Không ẩn được sản phẩm.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DatAnhChinh(int id, int maAnh)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var (ok, loi) = await _products.DatAnhChinhAsync(userId, id, maAnh);
        if (!ok) this.ThongBaoLoi(loi ?? "Không đặt được ảnh chính.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaAnh(int id, int maAnh)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var (ok, loi) = await _products.XoaAnhAsync(userId, id, maAnh);
        if (!ok) this.ThongBaoLoi(loi ?? "Không xóa được ảnh.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    // =====================================================================

    /// <summary>Gửi duyệt (B1) rồi phân tích từ khóa (B2 – bàn giao C2), sau đó quay về danh sách.</summary>
    private async Task<IActionResult> GuiDuyetVaChuyenVeIndexAsync(int userId, int maSanPham)
    {
        var (ok, loi) = await _products.GuiDuyetAsync(userId, maSanPham);
        if (!ok)
        {
            this.ThongBaoLoi(loi ?? "Không gửi duyệt được.");
            return RedirectToAction(nameof(Edit), new { id = maSanPham });
        }

        // C2: gọi ngay sau khi sp_GuiDuyetSanPham thành công; lỗi quét đã được service chuyển thành FAILED, không ném.
        await _moderation.PhanTichSanPhamAsync(maSanPham);

        this.ThongBaoThanhCong("Đã gửi sản phẩm đi kiểm duyệt. Admin sẽ duyệt trong thời gian tới.");
        return RedirectToAction(nameof(Index));
    }

    private async Task DienDanhMucAsync(ProductFormVm form)
    {
        form.DanhSachDanhMuc = (await _categories.GetCategoriesAsync()).ToList();
        form.DanhSachTinhTrang = (await _categories.GetConditionsAsync()).ToList();
    }

    /// <summary>Lưu file lên wwwroot/uploads/products; trả về danh sách (đường dẫn, dung lượng) cho service.</summary>
    private async Task<(List<ProductImageInput> Anh, string? Loi)> LuuAnhLenDiaAsync(List<IFormFile>? files)
    {
        var result = new List<ProductImageInput>();
        if (files is null || files.Count == 0) return (result, null);
        if (files.Count > SoAnhToiDa) return (result, $"Mỗi lần chỉ tải tối đa {SoAnhToiDa} ảnh.");

        var thu = DateTime.UtcNow.ToString("yyyyMM");
        var thuMuc = Path.Combine(_env.WebRootPath, "uploads", "products", thu);
        Directory.CreateDirectory(thuMuc);

        foreach (var file in files)
        {
            if (file.Length == 0) continue;
            if (file.Length > MaxAnhBytes) return (result, $"Ảnh \"{Path.GetFileName(file.FileName)}\" vượt quá 5MB.");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!DinhDangAnhChoPhep.Contains(ext))
                return (result, $"Định dạng \"{ext}\" không được hỗ trợ. Chỉ nhận jpg, png, webp, gif.");

            var ten = Guid.NewGuid().ToString("N") + ext;
            var duongDanDia = Path.Combine(thuMuc, ten);
            await using (var mo = System.IO.File.Create(duongDanDia))
            {
                await file.CopyToAsync(mo);
            }
            result.Add(new ProductImageInput($"/uploads/products/{thu}/{ten}", (int)file.Length));
        }
        return (result, null);
    }
}
