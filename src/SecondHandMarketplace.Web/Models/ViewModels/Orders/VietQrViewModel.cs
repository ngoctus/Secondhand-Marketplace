namespace SecondHandMarketplace.Web.Models.ViewModels.Orders;

public class VietQrViewModel
{
    public decimal SoTien { get; set; }
    public string NoiDung { get; set; } = "REMARKET THANHTOAN";
    public string NganHang { get; set; } = "MB Bank";
    public string MaNganHang { get; set; } = "970422"; // MB
    public string SoTaiKhoan { get; set; } = "0987654321";
    public string ChuTaiKhoan { get; set; } = "SAN THUONG MAI RE:MARKET";
    public int? MaDonHang { get; set; }
    public string? MaDon { get; set; }
    public string? DanhSachMaDonHang { get; set; }
    public string? ReturnUrl { get; set; }
    public bool ChoPhepXacNhan { get; set; } = false;

    public string QrUrl =>
        $"https://api.vietqr.io/image/{MaNganHang}-{SoTaiKhoan}-compact2.png?amount={(long)SoTien}&addInfo={Uri.EscapeDataString(NoiDung)}&accountName={Uri.EscapeDataString(ChuTaiKhoan)}";
}
