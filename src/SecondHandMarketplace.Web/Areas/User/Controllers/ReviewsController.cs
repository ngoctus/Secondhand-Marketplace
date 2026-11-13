using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Models.ViewModels.Reviews;
using SecondHandMarketplace.Web.Services.Reviews;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

/// <summary>
/// Quản lý đánh giá phía người mua (C3):
/// 1. Index: Liệt kê danh sách các đánh giá do chính người mua đã viết (kèm trạng thái, số sao, nội dung).
/// 2. Tao: Tiếp nhận đánh giá người bán sau khi đơn hàng chuyển sang trạng thái COMPLETED.
/// </summary>
public class ReviewsController(IReviewService reviewService) : UserBaseController
{
    /// <summary>
    /// Danh sách đánh giá người mua đã viết.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var vm = await reviewService.LayDanhGiaCuaNguoiMuaAsync(userId, page, 10, cancellationToken);
        return View(vm);
    }

    /// <summary>
    /// Tiếp nhận form tạo đánh giá mới cho đơn hàng COMPLETED.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tao(TaoDanhGiaVm model, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var userId = User.LayNguoiDungId()!.Value;

        if (!ModelState.IsValid)
        {
            var error = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Dữ liệu đánh giá không hợp lệ.";
            this.ThongBaoLoi(error);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("ChiTiet", "Orders", new { area = "User", id = model.MaDonHang });
        }

        var (success, errorMessage, _) = await reviewService.TaoDanhGiaAsync(
            model.MaDonHang,
            userId,
            model.SoSao,
            model.NoiDung,
            cancellationToken);

        if (success)
        {
            this.ThongBaoThanhCong("Đánh giá người bán thành công! Cảm ơn bạn đã đóng góp xây dựng cộng đồng Re:Market uy tín.");
        }
        else
        {
            this.ThongBaoLoi(errorMessage ?? "Không thể gửi đánh giá.");
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("ChiTiet", "Orders", new { area = "User", id = model.MaDonHang });
    }
}
