using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Admin.Controllers;

public class ProductsController : AdminBaseController
{
    // TODO: Kiểm duyệt sản phẩm
    public IActionResult Index() => View();
}
