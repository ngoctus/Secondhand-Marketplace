namespace SecondHandMarketplace.Web.Common;

/// <summary>
/// Nhận dạng ảnh bằng chữ ký đầu file (magic bytes). Đuôi file và Content-Type do trình duyệt gửi
/// đều đổi được, chỉ nội dung bytes là thật; nhờ vậy file đổi đuôi (.jpg giả, .php thật) bị chặn (B6).
/// </summary>
public static class AnhDinhDang
{
    /// <summary>Trả về "jpg" | "png" | "webp" | "gif", null nếu không phải ảnh được hỗ trợ.</summary>
    public static string? LoaiThuc(ReadOnlySpan<byte> dauFile)
    {
        if (dauFile.Length >= 3 && dauFile[0] == 0xFF && dauFile[1] == 0xD8 && dauFile[2] == 0xFF) return "jpg";

        if (dauFile.Length >= 8
            && dauFile[0] == 0x89 && dauFile[1] == 0x50 && dauFile[2] == 0x4E && dauFile[3] == 0x47
            && dauFile[4] == 0x0D && dauFile[5] == 0x0A && dauFile[6] == 0x1A && dauFile[7] == 0x0A) return "png";

        if (dauFile.Length >= 6 && dauFile[0] == 0x47 && dauFile[1] == 0x49 && dauFile[2] == 0x46
            && dauFile[3] == 0x38 && (dauFile[4] == 0x37 || dauFile[4] == 0x39) && dauFile[5] == 0x61) return "gif";

        // RIFF....WEBP
        if (dauFile.Length >= 12
            && dauFile[0] == 0x52 && dauFile[1] == 0x49 && dauFile[2] == 0x46 && dauFile[3] == 0x46
            && dauFile[8] == 0x57 && dauFile[9] == 0x45 && dauFile[10] == 0x42 && dauFile[11] == 0x50) return "webp";

        return null;
    }

    /// <summary>Đuôi file (không có dấu chấm) có khớp với loại ảnh nhận được từ nội dung hay không.</summary>
    public static bool DuKhopVoI(string? loaiThuc, string duoiFile) => (loaiThuc, duoiFile.ToLowerInvariant()) switch
    {
        ("jpg", "jpg") or ("jpg", "jpeg") or ("png", "png") or ("gif", "gif") or ("webp", "webp") => true,
        _ => false
    };
}
