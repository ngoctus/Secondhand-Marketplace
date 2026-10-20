using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

public class OrdersController : UserBaseController
{
    // TODO: Đơn hàng của tôi
    public IActionResult Index() => View();
}
