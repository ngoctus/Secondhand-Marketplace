using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

public class FavoritesController : UserBaseController
{
    // TODO: Sản phẩm yêu thích
    public IActionResult Index() => View();
}
