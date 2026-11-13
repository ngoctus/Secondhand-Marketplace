using SecondHandMarketplace.Web.Common;

namespace SecondHandMarketplace.Web.Models.ViewModels.Orders;

public class VietQrViewModel
{
    public decimal SoTien { get; set; }
    public string NoiDung { get; set; } = "REMARKET THANHTOAN";
    public string NganHang { get; set; } = "MB Bank";
    public string MaNganHang { get; set; } = "970422"; // MB Bank BIN
    public string SoTaiKhoan { get; set; } = "0987654321";
    public string ChuTaiKhoan { get; set; } = "SAN THUONG MAI RE:MARKET";
    public int? MaDonHang { get; set; }
    public string? MaDon { get; set; }
    public string? DanhSachMaDonHang { get; set; }
    public string? ReturnUrl { get; set; }
    public bool ChoPhepXacNhan { get; set; } = false;

    /// <summary>
    /// Chuỗi mã VietQR theo chuẩn EMVCo quốc tế (được sinh bởi thuật toán học từ thầy UTC2).
    /// </summary>
    public string QrPayload => VietQrGenerator.GeneratePayload(MaNganHang ?? NganHang, SoTaiKhoan, SoTien, NoiDung);

    /// <summary>
    /// Ảnh mã QR tạo offline cục bộ bằng thư viện QRCoder (Base64 PNG).
    /// Không cần internet, bảo mật 100%, quét app ngân hàng nhận diện đúng tiền và nội dung.
    /// </summary>
    public string QrDataUrl => VietQrGenerator.GenerateQrBase64(QrPayload);

    /// <summary>
    /// Nguồn ảnh QR ưu tiên hiển thị mã nội bộ QRCoder từ bài giảng của thầy.
    /// </summary>
    public string QrImageSource
    {
        get
        {
            var dataUrl = QrDataUrl;
            if (!string.IsNullOrEmpty(dataUrl)) return dataUrl;
            return $"https://api.vietqr.io/image/{MaNganHang}-{SoTaiKhoan}-compact2.png?amount={(long)SoTien}&addInfo={Uri.EscapeDataString(NoiDung)}&accountName={Uri.EscapeDataString(ChuTaiKhoan)}";
        }
    }

    /// <summary>
    /// Giữ tương thích ngược với code cũ nếu có chỗ gọi QrUrl.
    /// </summary>
    public string QrUrl => QrImageSource;
}
