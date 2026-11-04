using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public class CategoriesController : AdminBaseController
{
    // TODO: Quản lý danh mục
    public IActionResult Index() => View();
}
