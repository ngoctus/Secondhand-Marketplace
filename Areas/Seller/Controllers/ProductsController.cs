using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Seller.Controllers;

public class ProductsController : SellerBaseController
{
    // TODO: Sản phẩm của tôi
    public IActionResult Index() => View();
}
