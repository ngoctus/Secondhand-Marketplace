using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.ViewModels.Shop;
using SecondHandMarketplace.Web.Services.Areas;
using SecondHandMarketplace.Web.Services.Shops;
using SecondHandMarketplace.Web.Services.Storage;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

[Authorize(Roles = AppRoles.User)]
public sealed class ShopController(
    IShopService shops,
    IKhuVucService khuVuc,
    IUploadStorage uploadStorage) : UserBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.LayNguoiDungId() is not int userId) return Challenge();
        var shop = await shops.GetByOwnerAsync(userId, cancellationToken);
        // Shop cũ có các cột địa chỉ/ngân hàng = NULL vẫn mở được form; ô trống sẽ bắt bổ sung khi lưu.
        var model = shop is null ? new ShopVm() : new ShopVm
        {
            MaCuaHang = shop.MaCuaHang,
            TenShop = shop.TenShop,
            SoDienThoaiLienHe = shop.SoDienThoaiLienHe,
            MoTa = shop.MoTa,
            Logo = shop.Logo,
            TrangThai = shop.TrangThai,
            MaTinhThanh = shop.MaTinhThanh,
            MaPhuongXa = shop.MaPhuongXa,
            DiaChiChiTiet = shop.DiaChiChiTiet,
            KhuVucHienThi = shop.KhuVucHienThi,
            MaNganHang = shop.MaNganHang,
            NganHang = shop.NganHang,
            SoTaiKhoan = shop.SoTaiKhoan,
            TenChuTaiKhoan = shop.TenChuTaiKhoan
        };
        await NapDanhSachAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> Index(ShopVm model, IFormFile? logoFile, CancellationToken cancellationToken)
    {
        if (User.LayNguoiDungId() is not int userId) return Challenge();

        string? savedLogoPath = null;
        if (logoFile is { Length: > 0 })
        {
            var (hopLe, loiAnh, ext) = await UploadAnhAnToan.KiemTraAnhHopLeAsync(logoFile, cancellationToken: cancellationToken);
            if (!hopLe)
            {
                ModelState.AddModelError(nameof(model.Logo), loiAnh ?? "Ảnh đại diện cửa hàng không hợp lệ.");
            }
            else
            {
                var relPath = $"/uploads/shops/{Guid.NewGuid():N}{ext}";
                await using var stream = logoFile.OpenReadStream();
                await uploadStorage.SaveAsync(stream, relPath, isPrivate: false, UploadContentType(ext!), cancellationToken);
                model.Logo = relPath;
                savedLogoPath = relPath;
            }
        }

        if (!ModelState.IsValid)
        {
            if (savedLogoPath is not null)
                await uploadStorage.DeleteAsync(savedLogoPath, isPrivate: false, cancellationToken);
            return await HienLaiFormAsync(model, cancellationToken);
        }

        var result = await shops.SaveAsync(userId, new ShopSaveInput(
            model.TenShop, model.SoDienThoaiLienHe, model.MoTa, model.Logo,
            model.MaTinhThanh, model.MaPhuongXa, model.DiaChiChiTiet, model.KhuVucHienThi,
            model.MaNganHang, model.SoTaiKhoan, model.TenChuTaiKhoan), cancellationToken);
        if (!result.Success)
        {
            if (savedLogoPath is not null)
                await uploadStorage.DeleteAsync(savedLogoPath, isPrivate: false, cancellationToken);
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể lưu cửa hàng.");
            return await HienLaiFormAsync(model, cancellationToken);
        }

        await HomeCacheHelper.XoaCacheTrangChuAsync(
            HttpContext?.RequestServices.GetService<IDistributedCache>(),
            cancellationToken);
        TempData["ThanhCong"] = "Đã lưu thông tin cửa hàng.";
        return RedirectToAction(nameof(Index));
    }

    private static string UploadContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };

    private async Task<IActionResult> HienLaiFormAsync(ShopVm model, CancellationToken cancellationToken)
    {
        await NapDanhSachAsync(model, cancellationToken);
        return View(model);
    }

    /// <summary>Nạp danh sách tỉnh, xã của tỉnh đang chọn (để dropdown phụ thuộc hiển thị đúng sau khi post lỗi).</summary>
    private async Task NapDanhSachAsync(ShopVm model, CancellationToken cancellationToken)
    {
        model.TinhThanhs = await khuVuc.GetTinhThanhAsync(cancellationToken);
        model.PhuongXas = model.MaTinhThanh is > 0
            ? await khuVuc.GetPhuongXaTheoTinhAsync(model.MaTinhThanh.Value, cancellationToken)
            : Array.Empty<PhuongXaItem>();
    }
}
