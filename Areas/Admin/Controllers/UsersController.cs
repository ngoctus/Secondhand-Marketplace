using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public class UsersController : AdminBaseController
{
    // TODO: Quản lý người dùng
    public IActionResult Index() => View();
}
