using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

public class ReviewsController : UserBaseController
{
    // TODO: Đánh giá người bán
    public IActionResult Index() => View();
}
