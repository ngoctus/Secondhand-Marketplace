using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Admin.Controllers;

public class AuctionsController : AdminBaseController
{
    // TODO: Quản lý đấu giá
    public IActionResult Index() => View();
}
