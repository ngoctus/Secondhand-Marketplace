using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Seller.Controllers;

public class DashboardController : SellerBaseController
{
    // TODO: Thống kê của shop
    public IActionResult Index() => View();
}
