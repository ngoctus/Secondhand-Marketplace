using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Services.Favorites;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

/// <summary>B4 – Sản phẩm yêu thích.</summary>
public class FavoritesController : UserBaseController
{
    private readonly IFavoriteService _favorites;

    public FavoritesController(IFavoriteService favorites) => _favorites = favorites;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var userId = User.LayNguoiDungId()!.Value;
        return View(await _favorites.LayDanhSachAsync(userId, page));
    }

    /// <summary>Bật/tắt yêu thích. Bản tin AJAX (nút tim trên lưới sản phẩm) hoặc form thường.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int maSanPham, string? returnUrl = null)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var (thanhCong, laYeuThich) = await _favorites.ToggleAsync(userId, maSanPham);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = thanhCong, isFavorite = laYeuThich });
        }

        if (!thanhCong) this.ThongBaoLoi("Không thao tác được với sản phẩm yêu thích.");
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Danh sách Id đang yêu thích (JSON) để site.js đánh dấu nút tim.</summary>
    [HttpGet]
    public async Task<IActionResult> DanhSachIds()
    {
        var userId = User.LayNguoiDungId()!.Value;
        return Json(await _favorites.LayIdsDangYeuThichAsync(userId));
    }
}
