using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Services.Dashboard;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

public sealed class DashboardController(IDashboardService dashboard) : UserBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = User.LayNguoiDungId();
        if (userId is null) return Challenge();
        var vm = await dashboard.GetSellerAsync(userId.Value, ct);
        if (vm is null)
        {
            this.ThongBaoLoi("Bạn cần tạo cửa hàng để xem thống kê.");
            return RedirectToAction("Index", "Shop");
        }
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Chart(string mode = "day", CancellationToken ct = default)
    {
        var userId = User.LayNguoiDungId();
        if (userId is null) return Unauthorized();
        return Json(await dashboard.GetSellerChartAsync(userId.Value, mode, ct));
    }
}