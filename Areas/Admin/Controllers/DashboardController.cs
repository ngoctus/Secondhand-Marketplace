using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public class DashboardController : AdminBaseController
{
    // TODO: Thống kê hệ thống
    public IActionResult Index() => View();
}
