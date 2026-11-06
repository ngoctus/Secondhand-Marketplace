using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Services.ProductReview;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

/// <summary>C1: xem lịch sử vi phạm kiểm duyệt (chỉ đọc). Khóa/mở khóa tài khoản là việc của Admin/Users (A).</summary>
public sealed class ViolationsController(IProductReviewService reviews) : AdminBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(int? sellerId, int page = 1, CancellationToken cancellationToken = default)
    {
        if (sellerId is <= 0) return NotFound();
        return View(await reviews.GetViolationsAsync(sellerId, page, cancellationToken));
    }
}
