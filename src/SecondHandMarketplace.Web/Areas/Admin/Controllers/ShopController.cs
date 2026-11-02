using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.ViewModels.Shop;
using SecondHandMarketplace.Web.Services.Shops;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

[Authorize(Roles = AppRoles.User)]
public sealed class ShopController(IShopService shops) : UserBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.GetUserId(), out var userId)) return Challenge();
        var shop = await shops.GetByOwnerAsync(userId, cancellationToken);
        return View(shop is null ? new ShopVm() : new ShopVm
        {
            MaCuaHang = shop.MaCuaHang,
            TenShop = shop.TenShop,
            SoDienThoaiLienHe = shop.SoDienThoaiLienHe,
            MoTa = shop.MoTa,
            Logo = shop.Logo,
            TrangThai = shop.TrangThai
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ShopVm model, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.GetUserId(), out var userId)) return Challenge();
        if (!ModelState.IsValid) return View(model);
        var result = await shops.SaveAsync(userId, model.TenShop, model.SoDienThoaiLienHe, model.MoTa, model.Logo, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể lưu cửa hàng.");
            return View(model);
        }
        TempData["ThanhCong"] = "Đã lưu thông tin cửa hàng.";
        return RedirectToAction(nameof(Index));
    }
}