using System.Globalization;
using System.Text;

namespace SecondHandMarketplace.Web.Common;

/// <summary>
/// Chuẩn hóa chuỗi tìm kiếm giống hàm SQL dbo.fn_ChuanHoaChuoi:
/// chữ thường, bỏ dấu tiếng Việt, ký tự đặc biệt thành khoảng trắng, gộp khoảng trắng.
/// Dùng để so khớp với cột SanPham.ChuoiTimKiem.
/// </summary>
public static class TextHelper
{
    public static string ChuanHoa(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var formD = text.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);

        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }

        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}