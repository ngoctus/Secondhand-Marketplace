using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Seller.Controllers;

public class AuctionsController : SellerBaseController
{
    // TODO: Quản lý đấu giá
    public IActionResult Index() => View();
}
