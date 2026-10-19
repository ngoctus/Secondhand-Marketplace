using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Buyer.Controllers;

public class ReportsController : BuyerBaseController
{
    // TODO: Báo cáo vi phạm
    public IActionResult Index() => View();
}
