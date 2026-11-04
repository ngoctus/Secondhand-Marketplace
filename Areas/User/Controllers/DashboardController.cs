using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

public class DashboardController : UserBaseController
{
    // TODO: Thống kê cửa hàng (chỉ user đã kích hoạt shop)
    // Khi có policy Seller thì gắn [Authorize(Policy = "Seller")] cho controller này.
    public IActionResult Index() => View();
}
