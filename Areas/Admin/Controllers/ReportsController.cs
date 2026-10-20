using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public class ReportsController : AdminBaseController
{
    // TODO: Xử lý báo cáo vi phạm
    public IActionResult Index() => View();
}
