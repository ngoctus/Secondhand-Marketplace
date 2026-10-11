using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Admin.Controllers;

public class DashboardController : AdminBaseController
{
    // TODO: Thống kê hệ thống
    public IActionResult Index() => View();
}
