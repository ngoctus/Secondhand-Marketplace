using System.Globalization;

namespace SecondHandMarketplace.Web.Services.Ai;

/// <summary>
/// Chuẩn hóa mã ngôn ngữ (vi | en) dùng xuyên suốt Chatbox AI, gợi ý mô tả và gợi ý giá (C-03).
/// Ưu tiên giá trị client gửi rõ ràng; nếu trống thì đọc từ CultureInfo.CurrentUICulture (A-01 RequestLocalization).
/// </summary>
public static class AiLanguage
{
    public const string Vietnamese = "vi";
    public const string English = "en";

    public static string Normalize(string? language)
    {
        var raw = language?.Trim();
        if (!string.IsNullOrEmpty(raw))
        {
            if (raw.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return English;
            if (raw.StartsWith("vi", StringComparison.OrdinalIgnoreCase)) return Vietnamese;
        }

        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .Equals("en", StringComparison.OrdinalIgnoreCase)
            ? English
            : Vietnamese;
    }

    public static bool IsEnglish(string? language) => Normalize(language) == English;

    public static string Choose(string? language, string vi, string en) => IsEnglish(language) ? en : vi;
}
