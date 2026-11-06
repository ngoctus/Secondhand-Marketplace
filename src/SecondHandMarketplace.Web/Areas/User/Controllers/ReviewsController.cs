using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Models.ViewModels.Reviews;
using SecondHandMarketplace.Web.Services.Reviews;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

public class ReviewsController : UserBaseController
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService) => _reviewService = reviewService;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.LayNguoiDungId()!.Value;
        return View(await _reviewService.LayDanhSachChoDanhGiaAsync(userId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tao(TaoDanhGiaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            this.ThongBaoLoi("Dữ liệu đánh giá không hợp lệ.");
            return RedirectToAction(nameof(Index));
        }
        var result = await _reviewService.TaoDanhGiaAsync(User.LayNguoiDungId()!.Value, model);
        if (result.ThanhCong) this.ThongBaoThanhCong(result.ThongBao);
        else this.ThongBaoLoi(result.ThongBao);
        return RedirectToAction(nameof(Index));
    }
}
