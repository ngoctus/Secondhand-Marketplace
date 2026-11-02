using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Services.Shops;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public sealed class SellersController(AppDbContext db, IShopService shops) : AdminBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, CancellationToken cancellationToken)
    {
        var query = db.CuaHangs.AsNoTracking().Include(x => x.NguoiDung).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => x.TenShop.Contains(keyword) || x.NguoiDung.HoTen.Contains(keyword) || x.NguoiDung.Email.Contains(keyword));
        }
        if (status is "ACTIVE" or "LOCKED") query = query.Where(x => x.TrangThai == status);
        ViewBag.Search = search;
        ViewBag.Status = status;
        return View(await query.OrderByDescending(x => x.NgayTao).ToListAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(int id, string reason, CancellationToken cancellationToken)
    {
        var result = await shops.LockShopAsync(ParseCurrentUserId(), id, reason, cancellationToken);
        if (result.Success) TempData["ThanhCong"] = "Đã khóa cửa hàng.";
        else TempData["Loi"] = result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(int id, CancellationToken cancellationToken)
    {
        var result = await shops.UnlockShopAsync(ParseCurrentUserId(), id, cancellationToken);
        if (result.Success) TempData["ThanhCong"] = "Đã mở khóa cửa hàng.";
        else TempData["Loi"] = result.Error;
        return RedirectToAction(nameof(Index));
    }

    private int? ParseCurrentUserId() => int.TryParse(User.GetUserId(), out var id) ? id : null;
}