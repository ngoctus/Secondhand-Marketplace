using SecondHandMarketplace.Web.Common;

namespace SecondHandMarketplace.Web.Models.ViewModels.Orders;

public class VietQrViewModel
{
    public decimal SoTien { get; set; }
    public string NoiDung { get; set; } = "REMARKET THANHTOAN";
    public string NganHang { get; set; } = string.Empty;
    public string MaNganHang { get; set; } = string.Empty;
    public string SoTaiKhoan { get; set; } = string.Empty;
    public string ChuTaiKhoan { get; set; } = string.Empty;
    public int? MaDonHang { get; set; }
    public string? MaDon { get; set; }
    public string? DanhSachMaDonHang { get; set; }
    public string? ReturnUrl { get; set; }
    public bool ChoPhepXacNhan { get; set; } = false;

    /// <summary>
    /// Chuỗi mã VietQR theo chuẩn EMVCo quốc tế (được sinh bởi thuật toán học từ thầy UTC2).
    /// </summary>
    public bool CoThongTinTaiKhoan =>
        !string.IsNullOrWhiteSpace(NganHang) &&
        MaNganHang is { Length: 6 } && MaNganHang.All(char.IsDigit) &&
        SoTaiKhoan is { Length: >= 6 and <= 25 } && SoTaiKhoan.All(char.IsDigit) &&
        !string.IsNullOrWhiteSpace(ChuTaiKhoan);

    public string QrPayload => CoThongTinTaiKhoan
        ? VietQrGenerator.GeneratePayload(MaNganHang, SoTaiKhoan, SoTien, NoiDung)
        : string.Empty;

    /// <summary>
    /// Ảnh mã QR tạo offline cục bộ bằng thư viện QRCoder (Base64 PNG).
    /// Không cần internet, bảo mật 100%, quét app ngân hàng nhận diện đúng tiền và nội dung.
    /// </summary>
    public string QrDataUrl => CoThongTinTaiKhoan ? VietQrGenerator.GenerateQrBase64(QrPayload) : string.Empty;

    /// <summary>
    /// Nguồn ảnh QR ưu tiên hiển thị mã nội bộ QRCoder từ bài giảng của thầy.
    /// </summary>
    public string QrImageSource
    {
        get
        {
            return QrDataUrl;
        }
    }

    /// <summary>
    /// Giữ tương thích ngược với code cũ nếu có chỗ gọi QrUrl.
    /// </summary>
    public string QrUrl => QrImageSource;
}
