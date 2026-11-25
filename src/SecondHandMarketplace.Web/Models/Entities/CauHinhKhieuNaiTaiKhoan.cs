namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>
/// Bảng CauHinhKhieuNaiTaiKhoan (KN-01): cấu hình 1 dòng (Id = 1) cho giới hạn gửi, cooldown, mức phí lần 2 và tài khoản VietQR của sàn.
/// </summary>
public class CauHinhKhieuNaiTaiKhoan
{
    public int Id { get; set; } = 1;
    public int SoLanGuiMoiChuKy { get; set; } = 2;
    public int CooldownPhut { get; set; } = 4320;
    public decimal PhiMoKhoaLan2 { get; set; } = 200_000m;
    public string TenNganHang { get; set; } = "MB Bank";
    public string MaNganHang { get; set; } = "MB";
    public string SoTaiKhoan { get; set; } = "090123456789";
    public string ChuTaiKhoan { get; set; } = "RE MARKET PLATFORM";
    public DateTime NgayCapNhat { get; set; }
    public int? NguoiCapNhatId { get; set; }

    public NguoiDung? NguoiCapNhat { get; set; }
}
