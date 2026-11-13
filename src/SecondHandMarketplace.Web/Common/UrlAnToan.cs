namespace SecondHandMarketplace.Web.Common;

public static class UrlAnToan
{
    /// Chỉ cho phép http(s) tuyệt đối hoặc đường dẫn nội bộ bắt đầu bằng "/" (không phải "//" hay "/\").
    public static bool HopLe(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var v = value.Trim();
        if (v.Any(char.IsControl)) return false;
        if (v.StartsWith('/')) return !v.StartsWith("//") && !v.StartsWith("/\\");
        return Uri.TryCreate(v, UriKind.Absolute, out var u)
               && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
    }
}