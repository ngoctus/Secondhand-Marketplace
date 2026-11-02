namespace SecondHandMarketplace.Web.Services.Accounts;

public interface IUserAdminService
{
    Task<(bool Success, string? Error)> LockUserAsync(int? actorId, int userId, string? reason, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UnlockUserAsync(int? actorId, int userId, CancellationToken cancellationToken = default);
}