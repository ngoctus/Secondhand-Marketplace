using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Web2Hand.Data;
using Web2Hand.Hubs;
using Web2Hand.Models.Entities;
using Web2Hand.Models.Enums;

namespace Web2Hand.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationService(AppDbContext db, IHubContext<NotificationHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task CreateAsync(string userId, NotificationType type, string content, string? link = null)
    {
        _db.Notifications.Add(new Notification { UserId = userId, Type = type, Content = content, Link = link });
        await _db.SaveChangesAsync();

        await _hub.Clients.User(userId).SendAsync("ReceiveNotification", content, link);
    }

    public async Task<List<Notification>> GetLatestAsync(string userId, int take = 20)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task MarkAllReadAsync(string userId)
    {
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in unread)
            n.IsRead = true;
        await _db.SaveChangesAsync();
    }
}
