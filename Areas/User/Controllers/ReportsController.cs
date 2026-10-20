using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

public class ReportsController : UserBaseController
{
    // TODO: Báo cáo vi phạm
    public IActionResult Index() => View();
}
