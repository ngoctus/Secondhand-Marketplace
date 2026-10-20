using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

public class CartController : UserBaseController
{
    // TODO: Giỏ hàng
    public IActionResult Index() => View();
}
