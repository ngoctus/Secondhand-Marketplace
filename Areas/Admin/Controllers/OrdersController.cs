using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public class OrdersController : AdminBaseController
{
    // TODO: Quản lý đơn hàng
    public IActionResult Index() => View();
}
