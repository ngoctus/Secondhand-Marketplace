using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Services.Auctions;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public sealed class AuctionsController(IAuctionService auctions) : AdminBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(string? status, string? q, int page = 1, CancellationToken ct = default)
    {
        ViewBag.Status = status;
        ViewBag.Q = q;
        return View(await auctions.SearchAdminAsync(status, q, page, 15, ct));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var vm = await auctions.GetDetailAsync(id, null, maxBids: 500, anTen: false, ct: ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string reason, CancellationToken ct)
    {
        var adminId = User.LayNguoiDungId();
        if (adminId is null) return Challenge();
        var (ok, error) = await auctions.CancelAsync(adminId.Value, id, reason, ct);
        if (ok) this.ThongBaoThanhCong("Đã hủy phiên đấu giá và thông báo cho các bên liên quan.");
        else this.ThongBaoLoi(error ?? "Không hủy được phiên.");
        return RedirectToAction(nameof(Details), new { id });
    }
}