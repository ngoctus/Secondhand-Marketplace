namespace SecondHandMarketplace.Web.Services.Auth;

public interface IPasswordResetService
{
    Task<bool> SendCodeAsync(string email, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken cancellationToken = default);
}
