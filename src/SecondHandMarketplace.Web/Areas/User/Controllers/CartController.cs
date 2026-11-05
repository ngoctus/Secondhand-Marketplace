using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Services.Carts;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

public class CartController : UserBaseController
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>Xem giỏ hàng cá nhân.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.LayNguoiDungId()!.Value;
        var vm = await _cartService.LayGioHangAsync(userId);
        return View(vm);
    }

    /// <summary>Thêm một sản phẩm vào giỏ hàng.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(int maSanPham, string? returnUrl = null)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var (thanhCong, thongBao) = await _cartService.ThemVaoGioAsync(userId, maSanPham);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = thanhCong, message = thongBao });
        }

        if (thanhCong)
        {
            this.ThongBaoThanhCong(thongBao ?? "Đã thêm vào giỏ hàng.");
        }
        else
        {
            this.ThongBaoLoi(thongBao ?? "Không thể thêm vào giỏ hàng.");
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Bật/tắt chọn một món hàng.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChuyenTrangThaiChon(int maGioHang, bool daChon)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var ok = await _cartService.CapNhatTrangThaiChonAsync(userId, maGioHang, daChon);
        return Json(new { success = ok });
    }

    /// <summary>Bật/tắt chọn toàn bộ sản phẩm của một shop.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChonTatCaShop(int maCuaHang, bool daChon)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var ok = await _cartService.CapNhatChonTatCaTheoShopAsync(userId, maCuaHang, daChon);
        return Json(new { success = ok });
    }

    /// <summary>Xóa một món hàng khỏi giỏ.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int maGioHang)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var ok = await _cartService.XoaKhoiGioAsync(userId, maGioHang);

        if (ok)
        {
            this.ThongBaoThanhCong("Đã xóa sản phẩm khỏi giỏ hàng.");
        }
        else
        {
            this.ThongBaoLoi("Không thể xóa sản phẩm khỏi giỏ hàng.");
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Đếm số lượng món trong giỏ để cập nhật icon giỏ hàng trên thanh menu.</summary>
    [HttpGet]
    public async Task<IActionResult> DemSoLuong()
    {
        var userId = User.LayNguoiDungId();
        if (!userId.HasValue) return Json(new { count = 0 });

        var count = await _cartService.DemSoLuongMonAsync(userId.Value);
        return Json(new { count });
    }
}
