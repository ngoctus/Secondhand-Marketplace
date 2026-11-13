using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Services.Categories;
using SecondHandMarketplace.Web.Services.Moderation;
using SecondHandMarketplace.Web.Services.Products;
using SkiaSharp;

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
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService products, ICategoryService categories, IModerationService moderation, IWebHostEnvironment env, ILogger<ProductsController> logger)
    {
        _products = products;
        _categories = categories;
        _moderation = moderation;
        _env = env;
        _logger = logger;
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
            XoaAnhDaLuu(anh);
            await DienDanhMucAsync(form);
            return View(form);
        }

        var (ok, loi, maSanPham) = await _products.CreateAsync(userId, form, anh);
        if (!ok)
        {
            XoaAnhDaLuu(anh);
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

        var truoc = await _products.GetFormAsync(userId, id);

        var (anh, loiLuu) = await LuuAnhLenDiaAsync(AnhMoi);
        if (loiLuu is not null) ModelState.AddModelError(string.Empty, loiLuu);

        if (!ModelState.IsValid)
        {
            XoaAnhDaLuu(anh);
            form.AnhHienTai = truoc?.AnhHienTai ?? new();
            await DienDanhMucAsync(form);
            return View(form);
        }

        var (ok, loi) = await _products.UpdateAsync(userId, id, form, anh);
        if (!ok)
        {
            XoaAnhDaLuu(anh);
            ModelState.AddModelError(string.Empty, loi!);
            form.AnhHienTai = truoc?.AnhHienTai ?? new();
            await DienDanhMucAsync(form);
            return View(form);
        }

        // Sản phẩm đang bán mà sửa nội dung/ảnh thì phải duyệt lại để không lách kiểm duyệt.
        if (GuiDuyet == true || truoc?.TrangThai == "ACTIVE")
            return await GuiDuyetVaChuyenVeIndexAsync(userId, id);

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
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var (ok, loi) = await _products.DeleteAsync(userId, id);
        if (ok) this.ThongBaoThanhCong("Đã xóa sản phẩm thành công.");
        else this.ThongBaoLoi(loi ?? "Không thể xóa sản phẩm.");
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
        // Vẫn bọc thêm để lỗi hạ tầng (transaction/DB) không làm gián đoạn trải nghiệm người bán.
        try
        {
            await _moderation.PhanTichSanPhamAsync(maSanPham);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Phân tích từ khóa lỗi sau khi gửi duyệt sản phẩm {ProductId}.", maSanPham);
        }

        this.ThongBaoThanhCong("Đã gửi sản phẩm đi kiểm duyệt. Admin sẽ duyệt trong thời gian tới.");
        return RedirectToAction(nameof(Index));
    }

    private async Task DienDanhMucAsync(ProductFormVm form)
    {
        form.DanhSachDanhMuc = (await _categories.GetCategoriesAsync()).ToList();
        form.DanhSachTinhTrang = (await _categories.GetConditionsAsync()).ToList();
    }

    /// <summary>Xóa file vừa lưu nhưng không dùng được (validate/DB thất bại) để tránh file rác.</summary>
    private void XoaAnhDaLuu(IEnumerable<ProductImageInput> anh)
    {
        foreach (var a in anh)
        {
            var tuongDoi = a.DuongDan.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var duongDan = Path.Combine(_env.WebRootPath, tuongDoi);
            var thumbPath = Path.Combine(Path.GetDirectoryName(duongDan)!, Path.GetFileNameWithoutExtension(duongDan) + "-thumb.webp");
            try
            {
                if (System.IO.File.Exists(duongDan)) System.IO.File.Delete(duongDan);
                if (System.IO.File.Exists(thumbPath)) System.IO.File.Delete(thumbPath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
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

            // Đuôi file tự đổi được, nội dung thì không: 12 byte đầu phải đúng signature của ảnh.
            if (!await NoiDungAnhHopLeAsync(file, ext))
                return (result, $"Ảnh \"{Path.GetFileName(file.FileName)}\" có đuôi {ext} nhưng nội dung không phải ảnh {ext.TrimStart('.').ToUpperInvariant()}. Hãy lưu lại file đúng định dạng rồi tải lên.");

            var ten = Guid.NewGuid().ToString("N") + ext;
            var duongDanDia = Path.Combine(thuMuc, ten);
            var thumbnailPath = Path.Combine(thuMuc, Path.GetFileNameWithoutExtension(ten) + "-thumb.webp");
            await using (var mo = System.IO.File.Create(duongDanDia))
            {
                await file.CopyToAsync(mo);
            }

            try
            {
                using var source = System.IO.File.OpenRead(duongDanDia);
                using var codec = SKCodec.Create(source);
                var info = codec?.Info;
                if (info is null || info.Value.Width > 12000 || info.Value.Height > 12000 || (long)info.Value.Width * info.Value.Height > 80_000_000)
                    throw new InvalidDataException("Image dimensions exceed the allowed limit.");

                using var image = SKBitmap.Decode(codec!);
                if (image is null) throw new InvalidDataException("Image could not be decoded.");
                var scale = Math.Min(1d, 640d / Math.Max(image.Width, image.Height));
                var width = Math.Max(1, (int)Math.Round(image.Width * scale));
                var height = Math.Max(1, (int)Math.Round(image.Height * scale));
                using var thumbnail = image.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
                if (thumbnail is null) throw new InvalidDataException("Thumbnail could not be generated.");
                using var encoded = thumbnail.Encode(SKEncodedImageFormat.Webp, 78);
                if (encoded is null) throw new InvalidDataException("WebP encoding failed.");
                await using var output = System.IO.File.Create(thumbnailPath);
                encoded.SaveTo(output);
            }
            catch (Exception)
            {
                System.IO.File.Delete(duongDanDia);
                if (System.IO.File.Exists(thumbnailPath)) System.IO.File.Delete(thumbnailPath);
                return (result, $"Ảnh \"{Path.GetFileName(file.FileName)}\" không thể xử lý. Vui lòng tải lại ảnh hợp lệ.");
            }

            result.Add(new ProductImageInput($"/uploads/products/{thu}/{ten}", (int)file.Length));
        }
        return (result, null);
    }

    private static async Task<bool> NoiDungAnhHopLeAsync(IFormFile file, string extension)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header);
        return extension switch
        {
            ".jpg" or ".jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".webp" => read >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
            ".gif" => read >= 6 && header.AsSpan(0, 6).SequenceEqual("GIF87a"u8) || read >= 6 && header.AsSpan(0, 6).SequenceEqual("GIF89a"u8),
            _ => false
        };
    }
}
