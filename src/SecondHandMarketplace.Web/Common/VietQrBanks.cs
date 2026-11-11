namespace SecondHandMarketplace.Web.Common;

/// <summary>Một ngân hàng hỗ trợ chuyển khoản VietQR. <see cref="Bin"/> là mã BIN 6 chữ số dùng để sinh mã QR.</summary>
public sealed record NganHangVietQr(string Bin, string Ma, string TenNgan, string TenDayDu)
{
    public string TenHienThi => $"{TenNgan} - {TenDayDu}";
}

/// <summary>
/// Danh sách ngân hàng + mã BIN VietQR. Đối chiếu với danh sách VietQR/NAPAS (api.vietqr.io/v2/banks, các dòng
/// transferSupported = 1, bỏ ví điện tử MoMo/ZaloPay/MVAS) ngày 11/11/2026. Bản sao nguồn: database/data/vietqr_banks.json.
/// Khi NAPAS đổi BIN hoặc có ngân hàng mới, cập nhật tại đây (D4 sinh QR theo cột CuaHang.MaNganHang = Bin).
/// </summary>
public static class VietQrBanks
{
    public static readonly IReadOnlyList<NganHangVietQr> All = new NganHangVietQr[]
    {
        new("970436", "VCB", "Vietcombank", "Ngân hàng TMCP Ngoại Thương Việt Nam"),
        new("970415", "ICB", "VietinBank", "Ngân hàng TMCP Công thương Việt Nam"),
        new("970418", "BIDV", "BIDV", "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam"),
        new("970405", "VBA", "Agribank", "Ngân hàng Nông nghiệp và Phát triển Nông thôn Việt Nam"),
        new("970422", "MB", "MBBank", "Ngân hàng TMCP Quân đội"),
        new("970407", "TCB", "Techcombank", "Ngân hàng TMCP Kỹ thương Việt Nam"),
        new("970416", "ACB", "ACB", "Ngân hàng TMCP Á Châu"),
        new("970432", "VPB", "VPBank", "Ngân hàng TMCP Việt Nam Thịnh Vượng"),
        new("970423", "TPB", "TPBank", "Ngân hàng TMCP Tiên Phong"),
        new("970403", "STB", "Sacombank", "Ngân hàng TMCP Sài Gòn Thương Tín"),
        new("970437", "HDB", "HDBank", "Ngân hàng TMCP Phát triển Thành phố Hồ Chí Minh"),
        new("970441", "VIB", "VIB", "Ngân hàng TMCP Quốc tế Việt Nam"),
        new("970443", "SHB", "SHB", "Ngân hàng TMCP Sài Gòn - Hà Nội"),
        new("970448", "OCB", "OCB", "Ngân hàng TMCP Phương Đông"),
        new("970426", "MSB", "MSB", "Ngân hàng TMCP Hàng Hải"),
        new("970440", "SEAB", "SeABank", "Ngân hàng TMCP Đông Nam Á"),
        new("970431", "EIB", "Eximbank", "Ngân hàng TMCP Xuất Nhập khẩu Việt Nam"),
        new("970449", "LPB", "LPBank", "Ngân hàng TMCP Lộc Phát Việt Nam"),
        new("970428", "NAB", "NamABank", "Ngân hàng TMCP Nam Á"),
        new("970409", "BAB", "BacABank", "Ngân hàng TMCP Bắc Á"),
        new("970425", "ABB", "ABBANK", "Ngân hàng TMCP An Bình"),
        new("970412", "PVCB", "PVcomBank", "Ngân hàng TMCP Đại Chúng Việt Nam"),
        new("970438", "BVB", "BaoVietBank", "Ngân hàng TMCP Bảo Việt"),
        new("970433", "VIETBANK", "VietBank", "Ngân hàng TMCP Việt Nam Thương Tín"),
        new("970419", "NCB", "NCB", "Ngân hàng TMCP Quốc Dân"),
        new("970452", "KLB", "KienLongBank", "Ngân hàng TMCP Kiên Long"),
        new("970429", "SCB", "SCB", "Ngân hàng TMCP Sài Gòn"),
        new("970454", "VCCB", "VietCapitalBank", "Ngân hàng TMCP Bản Việt"),
        new("970430", "PGB", "PGBank", "Ngân hàng TMCP Thịnh vượng và Phát triển"),
        new("970400", "SGICB", "SaigonBank", "Ngân hàng TMCP Sài Gòn Công Thương"),
        new("970427", "VAB", "VietABank", "Ngân hàng TMCP Việt Á"),
        new("970414", "MBV", "MBV", "Ngân hàng TNHH MTV Việt Nam Hiện Đại"),
        new("970444", "CBB", "CBBank", "Ngân hàng TMTNHH MTV Ngoại thương Công nghệ số"),
        new("970446", "COOPBANK", "COOPBANK", "Ngân hàng Hợp tác xã Việt Nam"),
        new("970434", "IVB", "IndovinaBank", "Ngân hàng TNHH Indovina"),
        new("970424", "SHBVN", "ShinhanBank", "Ngân hàng TNHH MTV Shinhan Việt Nam"),
        new("970457", "WVN", "Woori", "Ngân hàng TNHH MTV Woori Việt Nam"),
        new("422589", "CIMB", "CIMB", "Ngân hàng TNHH MTV CIMB Việt Nam"),
        new("970462", "KBHN", "KookminHN", "Ngân hàng Kookmin - Chi nhánh Hà Nội"),
        new("970463", "KBHCM", "KookminHCM", "Ngân hàng Kookmin - Chi nhánh TP. Hồ Chí Minh"),
        new("668888", "KBank", "KBank", "Ngân hàng Đại chúng TNHH Kasikornbank"),
        new("546034", "CAKE", "CAKE", "Ngân hàng số CAKE by VPBank"),
        new("546035", "Ubank", "Ubank", "Ngân hàng số Ubank by VPBank"),
        new("963388", "TIMO", "Timo", "Ngân hàng số Timo by Ban Viet Bank"),
        new("971133", "PVDB", "PVcomBank Pay", "Ngân hàng số PVcomBank"),
    };

    public static NganHangVietQr? TimTheoBin(string? bin) =>
        string.IsNullOrWhiteSpace(bin) ? null : All.FirstOrDefault(b => b.Bin == bin.Trim());
}
