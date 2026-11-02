using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public class AuctionsController : AdminBaseController
{
    // TODO: Quản lý đấu giá
    public IActionResult Index() => View();
}
