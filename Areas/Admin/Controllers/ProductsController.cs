using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public class ProductsController : AdminBaseController
{
    // TODO: Kiểm duyệt sản phẩm
    public IActionResult Index() => View();
}
