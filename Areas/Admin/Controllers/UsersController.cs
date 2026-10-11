using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Admin.Controllers;

public class UsersController : AdminBaseController
{
    // TODO: Quản lý người dùng
    public IActionResult Index() => View();
}
