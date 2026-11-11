using System.Text;
using QRCoder;

namespace SecondHandMarketplace.Web.Common;

/// <summary>
/// Trình tạo mã VietQR theo chuẩn EMVCo và bài giảng Lập trình Web (UTC2).
/// Tạo chuỗi VietQR chuẩn và sinh mã QR Code nội bộ bằng thư viện QRCoder (không phụ thuộc dịch vụ ngoài).
/// </summary>
public static class VietQrGenerator
{
    /// <summary>
    /// Tạo chuỗi payload VietQR theo chuẩn EMVCo (TLVs + CRC16 ISO/IEC 13239).
    /// </summary>
    /// <param name="bankCode">Mã ngân hàng (VD: "MB", "VCB", "VIETINBANK", "970422"...)</param>
    /// <param name="accountNumber">Số tài khoản nhận tiền</param>
    /// <param name="amount">Số tiền thanh toán</param>
    /// <param name="message">Nội dung chuyển khoản (VD: "DH123")</param>
    /// <returns>Chuỗi VietQR text chuẩn EMVCo</returns>
    public static string GeneratePayload(string bankCode, string accountNumber, decimal amount, string message)
    {
        string bankBin = GetBankBin(bankCode);
        string amountStr = ((long)Math.Max(0, decimal.Round(amount))).ToString();
        string cleanMessage = BoDauTiengViet(message).Trim();
        if (cleanMessage.Length > 25) cleanMessage = cleanMessage[..25];

        // path12 (Bank ID + Account Number)
        string path12 = $"00{bankBin.Length:D2}{bankBin}01{accountNumber.Length:D2}{accountNumber}";

        // path11 (Merchant Account Information: GUID + Subfields + Service Code QRIBFTTA)
        string path11 = $"0010A00000072701{path12.Length:D2}{path12}0208QRIBFTTA";

        // path1 (Field 38)
        string path1 = $"38{path11.Length:D2}{path11}";

        // path21 (Additional Data - Field 62 subfield 08)
        string path21 = $"08{cleanMessage.Length:D2}{cleanMessage}";

        // path2 (Currency 704 [VND] + Amount + Country VN + Additional Data)
        string path2 = $"530370454{amountStr.Length:D2}{amountStr}5802VN62{path21.Length:D2}{path21}";

        // Chuỗi QR chưa có CRC (Payload Format 000201, Dynamic QR 010212)
        string rawWithoutCrc = $"000201010212{path1}{path2}6304";

        // Tính CRC16 và hoàn thiện QR code
        string crc = GenerateCheckSum(rawWithoutCrc).ToUpperInvariant();
        return rawWithoutCrc + crc;
    }

    /// <summary>
    /// Sinh ảnh QR Code dạng Base64 Data URL (data:image/png;base64,...) từ chuỗi VietQR
    /// bằng thư viện QRCoder (offline 100%, không cần kết nối mạng).
    /// </summary>
    public static string GenerateQrBase64(string qrCodeText, int pixelsPerModule = 10)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrCodeText, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrBytes = qrCode.GetGraphic(pixelsPerModule);
            return $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Sinh trọn gói ảnh mã QR giả định / VietQR chuẩn trực tiếp thành Data URL.
    /// </summary>
    public static string GenerateMockQrDataUrl(string bankCode, string accountNumber, decimal amount, string message)
    {
        string payload = GeneratePayload(bankCode, accountNumber, amount, message);
        return GenerateQrBase64(payload);
    }

    /// <summary>
    /// Tính toán CRC16 checksum theo chuẩn ISO/IEC 13239 (Đúng theo bài giảng của thầy).
    /// </summary>
    public static string GenerateCheckSum(string text)
    {
        ushort crc = 0xFFFF;
        ushort polynomial = 0x1021;
        byte[] bytes = Encoding.UTF8.GetBytes(text);

        foreach (byte b in bytes)
        {
            crc ^= (ushort)(b << 8);
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x8000) != 0)
                {
                    crc = (ushort)((crc << 1) ^ polynomial);
                }
                else
                {
                    crc = (ushort)(crc << 1);
                }
            }
        }

        return crc.ToString("X4");
    }

    /// <summary>
    /// Tra cứu mã BIN Napas của ngân hàng (Bảng mã ngân hàng chuẩn UTC2).
    /// </summary>
    public static string GetBankBin(string bankCode)
    {
        if (string.IsNullOrWhiteSpace(bankCode)) return "970422"; // Mặc định MB Bank

        // Nếu người dùng đã truyền trực tiếp mã BIN 6 chữ số
        if (bankCode.Length == 6 && int.TryParse(bankCode, out _))
            return bankCode;

        return bankCode.Trim().ToUpperInvariant() switch
        {
            // Các ngân hàng lớn
            "MB" or "MBBANK" or "MB BANK" => "970422",
            "ICB" or "VIETINBANK" or "VIETIN BANK" => "970415",
            "VCB" or "VIETCOMBANK" or "VIETCOM BANK" => "970436",
            "BIDV" => "970418",
            "VBA" or "AGRIBANK" or "AGRI BANK" => "970405",
            "TCB" or "TECHCOMBANK" or "TECHCOM BANK" => "970407",
            "ACB" => "970416",
            "VPB" or "VPBANK" or "VP BANK" => "970432",
            "TPB" or "TPBANK" or "TP BANK" => "970423",
            "STB" or "SACOMBANK" or "SACOM BANK" => "970403",
            "HDB" or "HDBANK" or "HD BANK" => "970437",
            "OCB" => "970448",
            "SHB" => "970443",
            "VIB" => "970441",
            "MSB" => "970426",
            "EIB" or "EXIMBANK" => "970431",
            "LPB" or "LPBANK" or "LIENVIETPOSTBANK" => "970449",
            "CAKE" => "546034",
            "TIMO" => "963388",
            _ => "970422" // Fallback MB Bank
        };
    }

    private static string BoDauTiengViet(string str)
    {
        if (string.IsNullOrEmpty(str)) return string.Empty;
        string[] arr1 = ["á", "à", "ả", "ã", "ạ", "â", "ấ", "ầ", "ẩ", "ẫ", "ậ", "ă", "ắ", "ằ", "ẳ", "ẵ", "ặ",
            "đ",
            "é","è","ẻ","ẽ","ẹ","ê","ế","ề","ể","ễ","ệ",
            "í","ì","ỉ","ĩ","ị",
            "ó","ò","ỏ","õ","ọ","ô","ố","ồ","ổ","ỗ","ộ","ơ","ớ","ờ","ở","ỡ","ợ",
            "ú","ù","ủ","ũ","ụ","ư","ứ","ừ","ử","ữ","ự",
            "ý","ỳ","ỷ","ỹ","ỵ"];
        string[] arr2 = ["a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a",
            "d",
            "e","e","e","e","e","e","e","e","e","e","e",
            "i","i","i","i","i",
            "o","o","o","o","o","o","o","o","o","o","o","o","o","o","o","o","o",
            "u","u","u","u","u","u","u","u","u","u","u",
            "y","y","y","y","y"];
        for (int i = 0; i < arr1.Length; i++)
        {
            str = str.Replace(arr1[i], arr2[i]);
            str = str.Replace(arr1[i].ToUpper(), arr2[i].ToUpper());
        }
        return str;
    }
}
