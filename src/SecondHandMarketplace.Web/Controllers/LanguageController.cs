using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Controllers;

/// <summary>
/// Chuyển đổi ngôn ngữ giao diện (vi-VN | en-US) lưu qua Cookie.
/// Hỗ trợ cả route /Language/SetLanguage và /Home/SetLanguage để tương thích với mọi View.
/// </summary>
public sealed class LanguageController : Controller
{
    private static readonly HashSet<string> SupportedCultures = new(StringComparer.OrdinalIgnoreCase)
    {
        "vi-VN",
        "en-US",
        "vi",
        "en"
    };

    [HttpPost("/Language/SetLanguage")]
    [HttpPost("/Home/SetLanguage")]
    [HttpGet("/Language/SetLanguage")]
    [HttpGet("/Home/SetLanguage")]
    public IActionResult SetLanguage(string culture, string? returnUrl = null)
    {
        var normalized = NormalizeCulture(culture);

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(normalized)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = false,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home", new { area = "" });
    }

    private static string NormalizeCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture) || !SupportedCultures.Contains(culture.Trim()))
            return "vi-VN";

        return culture.Trim().StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? "en-US"
            : "vi-VN";
    }
}
