using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Web2Hand.Hubs;

/// <summary>Chỉ dùng để server đẩy thông báo xuống (NotificationService). Client lắng nghe "ReceiveNotification".</summary>
[Authorize]
public class NotificationHub : Hub
{
}
