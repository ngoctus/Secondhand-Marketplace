using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Admin.Controllers;

public class OrdersController : AdminBaseController
{
    // TODO: Quản lý đơn hàng
    public IActionResult Index() => View();
}
