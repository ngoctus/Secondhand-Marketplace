using SecondHandMarketplace.Web.Models.Entities;

namespace SecondHandMarketplace.Web.Services.Auth;

/// <summary>
/// Các trạng thái xác thực đăng nhập (Task KN-05).
/// </summary>
public enum AuthResultStatus
{
    /// <summary>Đúng mật khẩu và tài khoản ACTIVE.</summary>
    Success,

    /// <summary>Sai email hoặc sai mật khẩu (chống dò email).</summary>
    InvalidCredentials,

    /// <summary>Nhập sai liên tiếp 5 lần, bị khóa tạm thời 15 phút.</summary>
    TempLockout,

    /// <summary>Đúng mật khẩu nhưng tài khoản đang ở trạng thái LOCKED.</summary>
    AccountLocked
}

/// <summary>
/// Kết quả chi tiết của AuthenticateWithStatusAsync.
/// </summary>
public sealed record AuthStatusResult
{
    public AuthResultStatus Status { get; init; }
    public NguoiDung? User { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? LockoutEnd { get; init; }

    public static AuthStatusResult Success(NguoiDung user) =>
        new() { Status = AuthResultStatus.Success, User = user };

    public static AuthStatusResult InvalidCredentials(string message = "Email hoặc mật khẩu không chính xác.") =>
        new() { Status = AuthResultStatus.InvalidCredentials, ErrorMessage = message };

    public static AuthStatusResult TempLockout(DateTime lockoutEnd, string message = "Bạn đã nhập sai mật khẩu quá nhiều lần. Vui lòng thử lại sau ít phút.") =>
        new() { Status = AuthResultStatus.TempLockout, LockoutEnd = lockoutEnd, ErrorMessage = message };

    public static AuthStatusResult AccountLocked(NguoiDung user, string message = "Tài khoản của bạn hiện đang bị khóa.") =>
        new() { Status = AuthResultStatus.AccountLocked, User = user, ErrorMessage = message };
}
