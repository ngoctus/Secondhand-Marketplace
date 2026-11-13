using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Controllers;

public sealed class HelpController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
