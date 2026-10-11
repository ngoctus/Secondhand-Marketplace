using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Buyer.Controllers;

public class OrdersController : BuyerBaseController
{
    // TODO: Đơn hàng của tôi
    public IActionResult Index() => View();
}
