using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web2Hand.Common.Extensions;
using Web2Hand.Services.Notifications;

namespace Web2Hand.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications) => _notifications = notifications;

    public async Task<IActionResult> Index()
        => View(await _notifications.GetLatestAsync(User.GetUserId()));

    [HttpPost]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notifications.MarkAllReadAsync(User.GetUserId());
        return RedirectToAction(nameof(Index));
    }
}
