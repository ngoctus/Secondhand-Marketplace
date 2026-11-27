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
    public async Task<IActionResult> Them(int maSanPham, int soLuong = 1, string? returnUrl = null)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var (thanhCong, thongBao) = await _cartService.ThemVaoGioAsync(userId, maSanPham, soLuong);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var cartCount = thanhCong ? await _cartService.DemSoLuongMonAsync(userId) : 0;
            return Json(new { success = thanhCong, message = thongBao, cartCount });
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

    /// <summary>Cập nhật số lượng một món hàng trong giỏ.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhatSoLuong(int maGioHang, int soLuong)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var (thanhCong, thongBao, soLuongMoi) = await _cartService.CapNhatSoLuongAsync(userId, maGioHang, soLuong);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = thanhCong, message = thongBao, soLuong = soLuongMoi });
        }

        if (!thanhCong) this.ThongBaoLoi(thongBao ?? "Không thể cập nhật số lượng.");
        else if (!string.IsNullOrEmpty(thongBao)) this.ThongBaoThanhCong(thongBao);

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaMonKhongKhaDung()
    {
        var userId = User.LayNguoiDungId()!.Value;
        var count = await _cartService.XoaMonKhongKhaDungAsync(userId);
        if (count > 0) this.ThongBaoThanhCong($"Đã xóa {count} món không còn khả dụng khỏi giỏ hàng.");
        else this.ThongBaoThanhCong("Giỏ hàng không có món nào cần dọn.");
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
