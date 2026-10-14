using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Buyer.Controllers;

public class ReviewsController : BuyerBaseController
{
    // TODO: Đánh giá người bán
    public IActionResult Index() => View();
}
