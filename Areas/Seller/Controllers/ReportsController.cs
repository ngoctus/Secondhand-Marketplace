using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Seller.Controllers;

public class ReportsController : SellerBaseController
{
    // TODO: Báo cáo vi phạm
    public IActionResult Index() => View();
}
