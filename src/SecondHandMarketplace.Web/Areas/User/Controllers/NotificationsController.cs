using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Models.ViewModels.Notifications;
using SecondHandMarketplace.Web.Services.Notifications;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

/// <summary>Trang thông báo của người dùng đang đăng nhập. Mã người dùng luôn lấy từ phiên đăng nhập.</summary>
public sealed class NotificationsController(INotificationService notifications) : UserBaseController
{
    private const int PageSize = 20;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (User.LayNguoiDungId() is not int userId) return Challenge();

        page = Math.Max(1, page);
        var result = await notifications.GetForUserAsync(userId, page, PageSize, cancellationToken);
        var unread = await notifications.CountUnreadAsync(userId, cancellationToken);
        return View(new NotificationListVm
        {
            Items = result.Items,
            UnreadCount = unread,
            Page = page,
            TotalItems = result.TotalItems,
            TotalPages = (int)Math.Ceiling(result.TotalItems / (double)PageSize)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, int page = 1, CancellationToken cancellationToken = default)
    {
        if (User.LayNguoiDungId() is not int userId) return Challenge();

        // Thông báo của người khác hoặc không tồn tại đều cho cùng một kết quả, không lộ thông tin.
        if (!await notifications.MarkAsReadAsync(userId, id, cancellationToken))
            this.ThongBaoLoi("Không tìm thấy thông báo.");
        return RedirectToAction(nameof(Index), new { page });
    }
}
