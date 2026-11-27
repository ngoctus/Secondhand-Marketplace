using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Services.Categories;
using SecondHandMarketplace.Web.Services.Moderation;
using SecondHandMarketplace.Web.Services.Products;
using SecondHandMarketplace.Web.Services.Storage;
using SkiaSharp;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

/// <summary>B1 – Người bán: đăng bán, sửa, ẩn, gửi duyệt sản phẩm (kèm ảnh).</summary>
public class ProductsController : UserBaseController
{
    private static readonly string[] DinhDangAnhChoPhep = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long MaxAnhBytes = 5 * 1024 * 1024;
    private const int SoAnhToiDa = 10;
    private const long MaxProductFormBytes = MaxAnhBytes * SoAnhToiDa + 1024 * 1024;

    private readonly IProductService _products;
    private readonly ICategoryService _categories;
    private readonly IModerationService _moderation;
    private readonly ILogger<ProductsController> _logger;
    private readonly IUploadStorage _uploadStorage;

    public ProductsController(IProductService products, ICategoryService categories, IModerationService moderation,
        IUploadStorage uploadStorage, ILogger<ProductsController> logger)
    {
        _products = products;
        _categories = categories;
        _moderation = moderation;
        _uploadStorage = uploadStorage;
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
    [RequestSizeLimit(MaxProductFormBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxProductFormBytes)]
    public async Task<IActionResult> Create(ProductFormVm form, List<IFormFile>? AnhMoi, bool? GuiDuyet, CancellationToken cancellationToken = default)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var (anh, loiLuu) = await LuuAnhLenKhoAsync(AnhMoi, cancellationToken);
        if (loiLuu is not null) ModelState.AddModelError(string.Empty, loiLuu);

        if (!ModelState.IsValid)
        {
            await XoaAnhDaLuuAsync(anh, cancellationToken);
            await DienDanhMucAsync(form);
            return View(form);
        }

        var (ok, loi, maSanPham) = await _products.CreateAsync(userId, form, anh);
        if (!ok)
        {
            await XoaAnhDaLuuAsync(anh, cancellationToken);
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
    [RequestSizeLimit(MaxProductFormBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxProductFormBytes)]
    public async Task<IActionResult> Edit(int id, ProductFormVm form, List<IFormFile>? AnhMoi, bool? GuiDuyet, CancellationToken cancellationToken = default)
    {
        var userId = User.LayNguoiDungId()!.Value;
        if (form.Id is null || form.Id != id) form.Id = id;

        var truoc = await _products.GetFormAsync(userId, id);

        var (anh, loiLuu) = await LuuAnhLenKhoAsync(AnhMoi, cancellationToken);
        if (loiLuu is not null) ModelState.AddModelError(string.Empty, loiLuu);

        if (!ModelState.IsValid)
        {
            await XoaAnhDaLuuAsync(anh, cancellationToken);
            form.AnhHienTai = truoc?.AnhHienTai ?? new();
            await DienDanhMucAsync(form);
            return View(form);
        }

        var (ok, loi) = await _products.UpdateAsync(userId, id, form, anh);
        if (!ok)
        {
            await XoaAnhDaLuuAsync(anh, cancellationToken);
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
        if (YeuCauTuAjax()) return Json(new { ok, error = ok ? null : loi });

        if (!ok) this.ThongBaoLoi(loi ?? "Không đặt được ảnh chính.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaAnh(int id, int maAnh)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var (ok, loi, maAnhChinhMoi) = await _products.XoaAnhAsync(userId, id, maAnh);
        if (YeuCauTuAjax()) return Json(new { ok, error = ok ? null : loi, coverId = maAnhChinhMoi });

        if (!ok) this.ThongBaoLoi(loi ?? "Không xóa được ảnh.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    // B-03: lưới ảnh trong form đăng/sửa gọi hai action trên bằng fetch để không mất nội dung
    // người bán đang soạn; request thường (tắt JS) vẫn nhận redirect như cũ.
    private bool YeuCauTuAjax() =>
        StringComparer.OrdinalIgnoreCase.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest");

    // =====================================================================

    /// <summary>Gửi duyệt (B1) rồi phân tích từ khóa (B2 – bàn giao C2), sau đó quay về danh sách.</summary>
    private async Task<IActionResult> GuiDuyetVaChuyenVeIndexAsync(int userId, int maSanPham)
    {
        var (ok, loi) = await _products.GuiDuyetAsync(userId, maSanPham);
        if (!ok)
        {
            // Tránh lách kiểm duyệt: nếu gửi duyệt thất bại, ẩn sản phẩm để nội dung chưa duyệt không còn hiển thị công khai.
            await _products.HideAsync(userId, maSanPham);
            this.ThongBaoLoi((loi ?? "Không gửi duyệt được.") + " Sản phẩm đã được chuyển về trạng thái ẩn để bảo vệ hệ thống.");
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
    private async Task XoaAnhDaLuuAsync(IEnumerable<ProductImageInput> anh, CancellationToken cancellationToken)
    {
        foreach (var a in anh)
        {
            try
            {
                await _uploadStorage.DeleteAsync(a.DuongDan, isPrivate: false, cancellationToken);
                await _uploadStorage.DeleteAsync(ThumbnailPath(a.DuongDan), isPrivate: false, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { }
        }
    }

    /// <summary>Kiểm tra ảnh, tạo thumbnail rồi lưu cả hai qua provider đã cấu hình.</summary>
    private async Task<(List<ProductImageInput> Anh, string? Loi)> LuuAnhLenKhoAsync(List<IFormFile>? files, CancellationToken cancellationToken)
    {
        var result = new List<ProductImageInput>();
        if (files is null || files.Count == 0) return (result, null);
        if (files.Count > SoAnhToiDa) return (result, $"Mỗi lần chỉ tải tối đa {SoAnhToiDa} ảnh.");

        var thu = DateTime.UtcNow.ToString("yyyyMM");

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

            byte[] originalBytes;
            byte[] thumbnailBytes;
            try
            {
                await using var uploadStream = file.OpenReadStream();
                using var original = new MemoryStream((int)file.Length);
                await uploadStream.CopyToAsync(original, cancellationToken);
                originalBytes = original.ToArray();
                using var source = new MemoryStream(originalBytes, writable: false);
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
                using var thumbStream = new MemoryStream();
                encoded.SaveTo(thumbStream);
                thumbnailBytes = thumbStream.ToArray();
            }
            catch (Exception)
            {
                return (result, $"Ảnh \"{Path.GetFileName(file.FileName)}\" không thể xử lý. Vui lòng tải lại ảnh hợp lệ.");
            }

            var ten = Guid.NewGuid().ToString("N") + ext;
            var relativePath = $"/uploads/products/{thu}/{ten}";
            try
            {
                await using var originalStream = new MemoryStream(originalBytes, writable: false);
                await _uploadStorage.SaveAsync(originalStream, relativePath, isPrivate: false, UploadContentType(ext), cancellationToken);
                await using var thumbnailStream = new MemoryStream(thumbnailBytes, writable: false);
                await _uploadStorage.SaveAsync(thumbnailStream, ThumbnailPath(relativePath), isPrivate: false, "image/webp", cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                try
                {
                    await _uploadStorage.DeleteAsync(relativePath, isPrivate: false, cancellationToken);
                    await _uploadStorage.DeleteAsync(ThumbnailPath(relativePath), isPrivate: false, cancellationToken);
                }
                catch (Exception cleanupEx) when (cleanupEx is not OperationCanceledException) { }
                return (result, "Không thể lưu ảnh lên kho lưu trữ. Vui lòng thử lại sau.");
            }
            result.Add(new ProductImageInput(relativePath, (int)file.Length));
        }
        return (result, null);
    }

    private static string ThumbnailPath(string imagePath)
    {
        var extension = Path.GetExtension(imagePath);
        return imagePath[..^extension.Length] + "-thumb.webp";
    }

    private static string UploadContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        _ => "application/octet-stream"
    };

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
