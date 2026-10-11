using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Seller.Controllers;

public class ReviewsController : SellerBaseController
{
    // TODO: Đánh giá về shop
    public IActionResult Index() => View();
}
