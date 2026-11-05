using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Services.Dashboard;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public sealed class DashboardController(IDashboardService dashboard) : AdminBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await dashboard.GetAdminAsync(ct));

    [HttpGet]
    public async Task<IActionResult> Chart(string mode = "day", CancellationToken ct = default) =>
        Json(await dashboard.GetAdminChartAsync(mode, ct));
}