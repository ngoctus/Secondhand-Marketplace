using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Account;

namespace SecondHandMarketplace.Web.Services.Auth;

public interface IRegistrationOtpService
{
    Task<(bool Success, string? Error, string? Token)> StartRegistrationAsync(RegisterVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, NguoiDung? User, string? NewToken)> VerifyOtpAsync(string token, string code, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, string? NewToken)> ResendCodeAsync(string token, CancellationToken cancellationToken = default);
}
