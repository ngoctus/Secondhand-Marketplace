using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Buyer.Controllers;

public class CartController : BuyerBaseController
{
    // TODO: Giỏ hàng
    public IActionResult Index() => View();
}
