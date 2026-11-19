using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.ViewModels.Notifications;
using SecondHandMarketplace.Web.Services.Notifications;
using SecondHandMarketplace.Web.Services.AuditLogs;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

/// <summary>Admin gửi thông báo hệ thống (loại SYSTEM) cho một người dùng hoặc toàn hệ thống.</summary>
public sealed class NotificationsController(INotificationService notifications, AppDbContext db, IAuditLogService auditLog) : AdminBaseController
{
    [HttpGet]
    public IActionResult Index() => View(new AdminSendNotificationVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(AdminSendNotificationVm vm, CancellationToken cancellationToken)
    {
        if (vm.Target is not (AdminSendNotificationVm.TargetUser or AdminSendNotificationVm.TargetAll))
            ModelState.AddModelError(nameof(vm.Target), "Đối tượng nhận không hợp lệ.");

        var email = vm.RecipientEmail?.Trim().ToLowerInvariant();
        if (vm.Target == AdminSendNotificationVm.TargetUser && string.IsNullOrEmpty(email))
            ModelState.AddModelError(nameof(vm.RecipientEmail), "Vui lòng nhập email người nhận.");

        if (!ModelState.IsValid) return View(nameof(Index), vm);

        if (vm.Target == AdminSendNotificationVm.TargetUser)
        {
            var recipientId = await db.NguoiDungs.AsNoTracking()
                .Where(x => x.Email == email)
                .Select(x => (int?)x.MaNguoiDung)
                .SingleOrDefaultAsync(cancellationToken);
            if (recipientId is null)
            {
                ModelState.AddModelError(nameof(vm.RecipientEmail), "Không tìm thấy người dùng với email này.");
                return View(nameof(Index), vm);
            }

            var (success, error) = await notifications.CreateAsync(
                recipientId.Value, NotificationTypes.System, vm.TieuDe, vm.NoiDung, cancellationToken: cancellationToken);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Không gửi được thông báo.");
                return View(nameof(Index), vm);
            }
            var adminId = User.LayNguoiDungId();
            if (adminId.HasValue)
            {
                await auditLog.LogAsync(adminId.Value, "SEND_NOTIFICATION", "NguoiDung", recipientId.Value, "SUCCESS", $"Gửi thông báo tới người dùng #{recipientId.Value} ({email}): {vm.TieuDe}");
            }
            this.ThongBaoThanhCong("Đã gửi thông báo tới người dùng.");
        }
        else
        {
            var (success, error, sent) = await notifications.CreateForAllUsersAsync(
                NotificationTypes.System, vm.TieuDe, vm.NoiDung, cancellationToken: cancellationToken);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Không gửi được thông báo.");
                return View(nameof(Index), vm);
            }
            var adminId = User.LayNguoiDungId();
            if (adminId.HasValue)
            {
                await auditLog.LogAsync(adminId.Value, "BROADCAST_NOTIFICATION", "ThongBao", null, "SUCCESS", $"Gửi thông báo toàn hệ thống tới {sent} người dùng: {vm.TieuDe}");
            }
            this.ThongBaoThanhCong($"Đã gửi thông báo tới {sent} người dùng.");
        }

        return RedirectToAction(nameof(Index));
    }
}
