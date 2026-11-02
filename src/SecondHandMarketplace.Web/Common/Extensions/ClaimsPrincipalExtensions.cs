using System.Security.Claims;

namespace SecondHandMarketplace.Web.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Lấy Id của user đang đăng nhập (chuỗi rỗng nếu chưa đăng nhập): User.GetUserId()</summary>
    public static string GetUserId(this ClaimsPrincipal user)
        => user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
}