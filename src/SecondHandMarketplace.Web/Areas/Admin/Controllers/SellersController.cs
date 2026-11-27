using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Services.Shops;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public sealed class SellersController(AppDbContext db, IShopService shops) : AdminBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 30, CancellationToken cancellationToken = default)
    {
        page = Math.Clamp(page, 1, 1000);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.CuaHangs.AsNoTracking()
            .Include(x => x.NguoiDung).Include(x => x.TinhThanh).Include(x => x.PhuongXa).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => x.TenShop.Contains(keyword) || x.NguoiDung.HoTen.Contains(keyword) || x.NguoiDung.Email.Contains(keyword));
        }
        if (status is "ACTIVE" or "LOCKED") query = query.Where(x => x.TrangThai == status);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.NgayTao)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Total = total;
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(int id, string reason, CancellationToken cancellationToken)
    {
        var result = await shops.LockShopAsync(User.LayNguoiDungId(), id, reason, cancellationToken);
        if (result.Success) TempData["ThanhCong"] = "Đã khóa cửa hàng.";
        else TempData["Loi"] = result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(int id, CancellationToken cancellationToken)
    {
        var result = await shops.UnlockShopAsync(User.LayNguoiDungId(), id, cancellationToken);
        if (result.Success) TempData["ThanhCong"] = "Đã mở khóa cửa hàng.";
        else TempData["Loi"] = result.Error;
        return RedirectToAction(nameof(Index));
    }
}