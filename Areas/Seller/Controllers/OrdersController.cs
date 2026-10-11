using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Seller.Controllers;

public class OrdersController : SellerBaseController
{
    // TODO: Đơn hàng của shop
    public IActionResult Index() => View();
}
