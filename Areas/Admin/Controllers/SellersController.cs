using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Admin.Controllers;

public class SellersController : AdminBaseController
{
    // TODO: Quản lý người bán
    public IActionResult Index() => View();
}
