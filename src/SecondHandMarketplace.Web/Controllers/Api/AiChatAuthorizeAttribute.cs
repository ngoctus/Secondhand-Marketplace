using System.Globalization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SecondHandMarketplace.Web.Common;

namespace SecondHandMarketplace.Web.Controllers.Api;

/// <summary>
/// Xác thực riêng cho API chat: chấp nhận JWT (Bearer) HOẶC Cookie, không cho Anonymous.
/// Dùng đúng hai scheme đã cấu hình trong Program.cs (không thay đổi cấu hình xác thực toàn cục).
///
/// Vì sao không dùng [Authorize(AuthenticationSchemes = "Cookies,Bearer")]: cần biết yêu cầu được xác thực bằng scheme nào
/// (Cookie thì bắt buộc kiểm tra CSRF, JWT thì không) và khi chưa đăng nhập phải trả 401 JSON; với [Authorize] thì scheme Cookie
/// sẽ chuyển hướng 302 sang trang đăng nhập.
///
/// CSRF: chỉ áp dụng cho yêu cầu xác thực bằng Cookie và phương thức không an toàn (POST...). Dùng Antiforgery token của ASP.NET Core,
/// widget gửi token qua header "RequestVerificationToken". JWT nằm trong header Authorization do mã JavaScript tự đặt nên trình duyệt
/// không tự gửi kèm ở yêu cầu từ trang khác, không cần token. Bearer sai/hết hạn không được dùng để lách CSRF: khi đó vẫn xét như Cookie.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AiChatAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;

        var jwt = await http.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (jwt.Succeeded && jwt.Principal?.Identity?.IsAuthenticated == true)
        {
            http.User = jwt.Principal;
            return;
        }

        var cookie = await http.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!cookie.Succeeded || cookie.Principal?.Identity?.IsAuthenticated != true)
        {
            http.Response.Headers.WWWAuthenticate = JwtBearerDefaults.AuthenticationScheme;
            context.Result = new ObjectResult(KetQua.ThatBai("Bạn cần đăng nhập để sử dụng trợ lý AI."))
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        // Antiforgery gắn token với người dùng hiện tại, nên phải gán User trước khi kiểm tra.
        http.User = cookie.Principal;

        if (RequiresCsrfCheck(http.Request.Method))
        {
            var antiforgery = http.RequestServices.GetRequiredService<IAntiforgery>();
            try
            {
                await antiforgery.ValidateRequestAsync(http);
            }
            catch (AntiforgeryValidationException)
            {
                context.Result = new ObjectResult(KetQua.ThatBai("Phiên làm việc không hợp lệ. Vui lòng tải lại trang rồi thử lại."))
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
        }
    }

    private static bool RequiresCsrfCheck(string method) =>
        !(HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method));
}
