using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public class SellersController : AdminBaseController
{
    // TODO: Quản lý người bán
    public IActionResult Index() => View();
}
