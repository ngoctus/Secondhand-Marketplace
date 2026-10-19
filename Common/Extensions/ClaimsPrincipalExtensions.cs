using System.Security.Claims;

namespace Web2Hand.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Lấy Id của user đang đăng nhập: User.GetUserId()</summary>
    public static string GetUserId(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
