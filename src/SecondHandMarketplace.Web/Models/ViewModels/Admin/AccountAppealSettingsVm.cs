using SecondHandMarketplace.Web.Services.Accounts;

namespace SecondHandMarketplace.Web.Models.ViewModels.Admin;

public sealed class AccountAppealSettingsVm
{
    public int SoLanGuiMoiChuKy { get; set; } = AccountAppealRules.DefaultMaxAttemptsPerCycle;
    public int CooldownNgay { get; set; } = 3;
    public int CooldownGio { get; set; }
    public int CooldownPhutLe { get; set; }
    public decimal PhiMoKhoaLan2 { get; set; } = AccountAppealRules.DefaultSecondLockFee;
    public string TenNganHang { get; set; } = AccountAppealRules.DefaultBankName;
    public string MaNganHang { get; set; } = AccountAppealRules.DefaultBankCode;
    public string SoTaiKhoan { get; set; } = AccountAppealRules.DefaultBankAccount;
    public string ChuTaiKhoan { get; set; } = AccountAppealRules.DefaultBankOwner;

    // Thông tin hiển thị thêm
    public int CooldownPhutHienTai { get; set; }
    public DateTime? NgayCapNhat { get; set; }
    public string? TenNguoiCapNhat { get; set; }
    public string PreviewTransferContent { get; set; } = "MOKHOA U1 L2";
    public string PreviewQrDataUrl { get; set; } = string.Empty;
}
