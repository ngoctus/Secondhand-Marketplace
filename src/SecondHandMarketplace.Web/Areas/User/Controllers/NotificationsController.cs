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

        var success = await notifications.MarkAsReadAsync(userId, id, cancellationToken);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var unread = await notifications.CountUnreadAsync(userId, cancellationToken);
            return Json(new { success, unreadCount = unread });
        }

        // Thông báo của người khác hoặc không tồn tại đều cho cùng một kết quả, không lộ thông tin.
        if (!success)
            this.ThongBaoLoi("Không tìm thấy thông báo.");
        return RedirectToAction(nameof(Index), new { page });
    }

    /// <summary>Đánh dấu tất cả thông báo là đã đọc (Task C-02).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken = default)
    {
        if (User.LayNguoiDungId() is not int userId) return Challenge();

        var count = await notifications.MarkAllAsReadAsync(userId, cancellationToken);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, count, unreadCount = 0 });
        }

        this.ThongBaoThanhCong("Đã đánh dấu tất cả thông báo là đã đọc.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Điều hướng thông minh khi click vào thông báo (tự động đánh dấu đã đọc và mở đúng trang chi tiết theo vai trò).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ChiTiet(int id, CancellationToken cancellationToken = default)
    {
        if (User.LayNguoiDungId() is not int userId) return Challenge();

        await notifications.MarkAsReadAsync(userId, id, cancellationToken);
        var url = await notifications.ResolveTargetUrlAsync(userId, id, cancellationToken);
        return Redirect(url);
    }

    /// <summary>Lấy danh sách 5 thông báo mới nhất cho Dropdown xem nhanh (Task C-02).</summary>
    [HttpGet]
    public async Task<IActionResult> QuickList(CancellationToken cancellationToken = default)
    {
        if (User.LayNguoiDungId() is not int userId) return Challenge();

        var result = await notifications.GetForUserAsync(userId, page: 1, pageSize: 5, cancellationToken);
        var unread = await notifications.CountUnreadAsync(userId, cancellationToken);

        var items = result.Items.Select(n => new
        {
            id = n.MaThongBao,
            loai = n.Loai,
            tieuDe = n.TieuDe,
            noiDung = n.NoiDung,
            daDoc = n.DaDoc,
            ngayTao = n.NgayTao.ToString("dd/MM/yyyy HH:mm"),
            url = $"/User/Notifications/ChiTiet/{n.MaThongBao}"
        });

        return Json(new { items, unreadCount = unread });
    }
}
