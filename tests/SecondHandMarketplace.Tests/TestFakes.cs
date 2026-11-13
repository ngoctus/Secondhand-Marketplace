using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.Accounts;
using SecondHandMarketplace.Web.Services.AuditLogs;
using SecondHandMarketplace.Web.Services.Notifications;
using SecondHandMarketplace.Web.Services.Shops;

namespace SecondHandMarketplace.Tests;

public class FakeNotificationService : INotificationService
{
    public List<(int UserId, string Type, string Title, string Content, int? RefId)> Notifications { get; } = new();

    public Task<(bool Success, string? Error)> CreateAsync(int userId, string type, string title, string content, int? referenceId = null, CancellationToken cancellationToken = default)
    {
        Notifications.Add((userId, type, title, content, referenceId));
        return Task.FromResult((true, (string?)null));
    }

    public Task<(bool Success, string? Error, int SentCount)> CreateForAllUsersAsync(string type, string title, string content, int? referenceId = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult((true, (string?)null, 1));
    }

    public Task<NotificationPage> GetForUserAsync(int userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new NotificationPage(new List<ThongBao>(), 0));
    }

    public Task<int> CountUnreadAsync(int userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public Task<bool> MarkAsReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}

public class FakeAuditLogService : IAuditLogService
{
    public List<(int? ActorId, string Action, string TargetType, int? TargetId, string Result, string? Reason)> Logs { get; } = new();

    public Task LogAsync(int? actorId, string action, string targetType, int? targetId = null, string result = "SUCCESS", string? reason = null, string? detail = null)
    {
        Logs.Add((actorId, action, targetType, targetId, result, reason));
        return Task.CompletedTask;
    }
}

public class FakeUserAdminService : IUserAdminService
{
    public List<(int? ActorId, int UserId, string? Reason)> LockedUsers { get; } = new();
    public (bool Success, string? Error) LockResult { get; set; } = (true, null);

    public Task<(bool Success, string? Error)> LockUserAsync(int? actorId, int userId, string? reason, CancellationToken cancellationToken = default)
    {
        LockedUsers.Add((actorId, userId, reason));
        return Task.FromResult(LockResult);
    }

    public Task<(bool Success, string? Error)> UnlockUserAsync(int? actorId, int userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult((true, (string?)null));
    }
}

public class FakeShopService : IShopService
{
    public List<(int? ActorId, int ShopId, string? Reason)> LockedShops { get; } = new();
    public (bool Success, string? Error) LockResult { get; set; } = (true, null);

    public Task<CuaHang?> GetByOwnerAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult<CuaHang?>(null);
    public Task<(bool Success, string? Error)> SaveAsync(int userId, ShopSaveInput input, CancellationToken cancellationToken = default) => Task.FromResult((true, (string?)null));
    public Task<SecondHandMarketplace.Web.Models.ViewModels.Shop.ShopPublicVm?> GetPublicShopAsync(int id, int page = 1, int pageSize = 12, CancellationToken cancellationToken = default) => Task.FromResult<SecondHandMarketplace.Web.Models.ViewModels.Shop.ShopPublicVm?>(null);
    
    public Task<(bool Success, string? Error)> LockShopAsync(int? actorId, int shopId, string? reason, CancellationToken cancellationToken = default)
    {
        LockedShops.Add((actorId, shopId, reason));
        return Task.FromResult(LockResult);
    }

    public Task<(bool Success, string? Error)> UnlockShopAsync(int? actorId, int shopId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult((true, (string?)null));
    }
}
