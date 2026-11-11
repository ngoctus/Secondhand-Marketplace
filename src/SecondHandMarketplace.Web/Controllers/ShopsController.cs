using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Services.Reviews;
using SecondHandMarketplace.Web.Services.Shops;

namespace SecondHandMarketplace.Web.Controllers;

/// <summary>Trang shop công khai: /Shops/Details/{id}, ai cũng xem được (không cần đăng nhập).</summary>
[AllowAnonymous]
public sealed class ShopsController(IShopService shops, IReviewService reviews) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Details(int id, int page = 1, int reviewPage = 1, CancellationToken cancellationToken = default)
    {
        var shop = await shops.GetPublicShopAsync(id, page, 12, cancellationToken);
        if (shop is null) return NotFound();   // không tồn tại, bị khóa hoặc chủ shop không còn hoạt động

        var shopReviews = await reviews.LayDanhGiaTheoShopAsync(id, reviewPage, 5, cancellationToken: cancellationToken);
        ViewBag.ShopReviews = shopReviews;

        return View(shop);
    }
}
