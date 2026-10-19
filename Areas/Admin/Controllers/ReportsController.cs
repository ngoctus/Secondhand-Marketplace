using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Admin.Controllers;

public class ReportsController : AdminBaseController
{
    // TODO: Xử lý báo cáo vi phạm
    public IActionResult Index() => View();
}
