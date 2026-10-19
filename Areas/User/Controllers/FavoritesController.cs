using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Buyer.Controllers;

public class FavoritesController : BuyerBaseController
{
    // TODO: Sản phẩm yêu thích
    public IActionResult Index() => View();
}
