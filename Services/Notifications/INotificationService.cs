using Web2Hand.Models.Entities;
using Web2Hand.Models.Enums;

namespace Web2Hand.Services.Notifications;

public interface INotificationService
{
    /// <summary>Lưu thông báo vào DB và đẩy realtime tới user (nếu đang online).</summary>
    Task CreateAsync(string userId, NotificationType type, string content, string? link = null);
    Task<List<Notification>> GetLatestAsync(string userId, int take = 20);
    Task MarkAllReadAsync(string userId);
}
