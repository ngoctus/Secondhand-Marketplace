using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.ViewModels.Admin;
using SecondHandMarketplace.Web.Services.Moderation;
using SecondHandMarketplace.Web.Services.ProductReview;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

/// <summary>C1: hàng chờ duyệt, chi tiết + cảnh báo C2, duyệt/từ chối, gỡ sản phẩm vi phạm. Chỉ ADMIN (AdminBaseController).</summary>
public sealed class ProductsController(IProductReviewService reviews) : AdminBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(string? tab, int page = 1, CancellationToken cancellationToken = default) =>
        View(await reviews.GetListAsync(tab, page, cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();
        var detail = await reviews.GetDetailAsync(id, cancellationToken);
        return detail is null ? NotFound() : View(detail);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();
        if (!TryGetAdminId(out var adminId)) return Forbid();

        var result = await reviews.ApproveAsync(adminId, id, cancellationToken);
        return Report(result, id, "Đã duyệt sản phẩm.", ProductReviewTabs.Pending);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, ReviewReasonVm input, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();
        if (!TryGetAdminId(out var adminId)) return Forbid();
        if (!ModelState.IsValid) return InvalidInput(id);

        var result = await reviews.RejectAsync(adminId, id, input.Reason, cancellationToken);
        return Report(result, id, "Đã từ chối sản phẩm.", ProductReviewTabs.Pending);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id, ReviewReasonVm input, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();
        if (!TryGetAdminId(out var adminId)) return Forbid();
        if (!ModelState.IsValid) return InvalidInput(id);

        var result = await reviews.RemoveProductAsync(adminId, id, input.Reason, cancellationToken);
        return Report(result, id, "Đã gỡ sản phẩm vi phạm.", ProductReviewTabs.Active);
    }

    private IActionResult InvalidInput(int id)
    {
        TempData["Loi"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        return RedirectToAction(nameof(Details), new { id });
    }

    private IActionResult Report(ProductDecisionResult result, int id, string successMessage, string tab)
    {
        if (!result.Success)
        {
            TempData["Loi"] = result.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        var message = successMessage;
        if (result.FollowUp is { SellerLockedNow: true } f)
            message += $" Người bán đạt {f.ViolationCount}/{ModerationPolicy.ViolationLockThreshold} vi phạm nên tài khoản đã bị khóa tự động.";
        if (!string.IsNullOrEmpty(result.Warning)) message += " " + result.Warning;
        TempData["ThanhCong"] = message;
        return RedirectToAction(nameof(Index), new { tab });
    }

    private bool TryGetAdminId(out int adminId) => int.TryParse(User.GetUserId(), out adminId) && adminId > 0;
}
