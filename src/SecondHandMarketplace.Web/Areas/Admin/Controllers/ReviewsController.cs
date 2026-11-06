using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Services.Reviews;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public class ReviewsController : AdminBaseController
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService) => _reviewService = reviewService;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1) => View(await _reviewService.LayDanhSachAdminAsync(page));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XuLy(int maDanhGia, string trangThai, string? ghiChu)
    {
        var result = await _reviewService.DuyetAsync(maDanhGia, User.LayNguoiDungId()!.Value, trangThai, ghiChu);
        if (result.ThanhCong) this.ThongBaoThanhCong(result.ThongBao);
        else this.ThongBaoLoi(result.ThongBao);
        return RedirectToAction(nameof(Index));
    }
}
