namespace SecondHandMarketplace.Web.Services.Auth;

public interface IPasswordResetService
{
    Task<bool> SendCodeAsync(string email, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, string? VerifiedToken)> VerifyCodeAsync(string email, string code, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> ResetPasswordWithTokenAsync(string email, string verifiedToken, string newPassword, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> ConsumeVerifiedTokenAsync(string email, string verifiedToken, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken cancellationToken = default);
}
