using SecondHandMarketplace.Web.Models.Entities;

namespace SecondHandMarketplace.Web.Services.Auth;

public interface IAuthService
{
    Task<NguoiDung?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, NguoiDung? User)> RegisterAsync(string fullName, string email, string phone, string password, CancellationToken cancellationToken = default);
    Task<NguoiDung?> GetUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateProfileAsync(int userId, string fullName, string? phone, string? address, string? avatar, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}
