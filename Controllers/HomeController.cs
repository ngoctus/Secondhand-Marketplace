using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Controllers;

public class HomeController : Controller
{
    // TODO: hiện banner (IBannerService), sản phẩm mới, phiên đấu giá sắp kết thúc
    public IActionResult Index() => View();

    public IActionResult Error() => View();
}
