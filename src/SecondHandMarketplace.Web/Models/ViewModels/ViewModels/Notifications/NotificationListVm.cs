using SecondHandMarketplace.Web.Models.Entities;

namespace SecondHandMarketplace.Web.Models.ViewModels.Notifications;

public sealed class NotificationListVm
{
    public IReadOnlyList<ThongBao> Items { get; set; } = [];
    public int UnreadCount { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
}
