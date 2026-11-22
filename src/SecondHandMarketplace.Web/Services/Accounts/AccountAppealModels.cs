namespace SecondHandMarketplace.Web.Services.Accounts;

/// <summary>
/// DTO dùng chung giữa tầng User (KN-06/KN-07) và tầng Admin (KN-03) cho chức năng Khiếu nại & Mở khóa tài khoản.
/// </summary>
public sealed record AccountAppealSettingsDto(
    int SoLanGuiMoiChuKy,
    int CooldownPhut,
    int CooldownNgay,
    int CooldownGio,
    int CooldownPhutLe,
    decimal PhiMoKhoaLan2,
    string TenNganHang,
    string MaNganHang,
    string SoTaiKhoan,
    string ChuTaiKhoan,
    DateTime NgayCapNhat,
    string? TenNguoiCapNhat);

public sealed class UpdateAccountAppealSettingsRequest
{
    public int SoLanGuiMoiChuKy { get; set; } = AccountAppealRules.DefaultMaxAttemptsPerCycle;
    public int CooldownNgay { get; set; }
    public int CooldownGio { get; set; }
    public int CooldownPhutLe { get; set; }
    public decimal PhiMoKhoaLan2 { get; set; } = AccountAppealRules.DefaultSecondLockFee;
    public string TenNganHang { get; set; } = AccountAppealRules.DefaultBankName;
    public string MaNganHang { get; set; } = AccountAppealRules.DefaultBankCode;
    public string SoTaiKhoan { get; set; } = AccountAppealRules.DefaultBankAccount;
    public string ChuTaiKhoan { get; set; } = AccountAppealRules.DefaultBankOwner;
}

public sealed record UnlockFeeQrDto(
    string TenNganHang,
    string MaNganHang,
    string SoTaiKhoan,
    string ChuTaiKhoan,
    decimal SoTien,
    string NoiDungChuyenKhoan,
    string QrDataUrl);

public sealed record LockedViolationItemDto(
    string Nguon,
    string MoTa,
    DateTime ThoiGian);

public sealed record AccountAppealSummaryDto(
    int MaKhieuNai,
    string LoaiYeuCau,
    int LanKhoaThu,
    string NoiDung,
    string? BangChungAnh,
    bool YeuCauPhi,
    decimal SoTienPhi,
    string? MaGiaoDichThamChieu,
    string TrangThai,
    string? GhiChuAdmin,
    DateTime NgayTao,
    DateTime? NgayXuLy);

public sealed record LockedAccountOverviewDto(
    int MaNguoiDung,
    string HoTen,
    string Email,
    bool DangBiKhoa,
    int LanKhoaHienTai,
    string? LyDoKhoa,
    DateTime? ThoiGianKhoa,
    int SoViPhamHieuLuc,
    int NguongKhoa,
    IReadOnlyList<LockedViolationItemDto> ViPhamHieuLuc,
    bool ChoPhepKhieuNaiKhoaNham,
    bool ChoPhepCamKetMoKhoa,
    bool YeuCauPhiLan2,
    UnlockFeeQrDto? ThongTinPhiQr,
    bool CoDonDangCho,
    int SoLuotDaGuiChuKy,
    int SoLuotToiDaChuKy,
    int SoLuotConLaiChuKy,
    bool DangTrongCooldown,
    DateTime? CooldownKetThucUtc,
    int CooldownPhutCauHinh,
    AccountAppealSummaryDto? DonGanNhat,
    IReadOnlyList<AccountAppealSummaryDto> LichSuDonDotNay)
{
    public bool CoTheGuiDonMoi => DangBiKhoa && !CoDonDangCho && !DangTrongCooldown && SoLuotConLaiChuKy > 0;
}

public sealed record SubmitAccountAppealResult(
    bool Success,
    string? Error,
    int? MaKhieuNai = null)
{
    public static SubmitAccountAppealResult Ok(int maKhieuNai) => new(true, null, maKhieuNai);
    public static SubmitAccountAppealResult Fail(string error) => new(false, error, null);
}

public sealed record AdminAccountAppealFilter(
    string? TrangThai = null,
    string? LoaiYeuCau = null,
    string? TuKhoa = null,
    int Page = 1);

public sealed record AdminAccountAppealRowDto(
    int MaKhieuNai,
    int MaNguoiDung,
    string HoTen,
    string Email,
    string TrangThaiTaiKhoan,
    int SoLanBiKhoaHienTai,
    string LoaiYeuCau,
    int LanKhoaThu,
    bool YeuCauPhi,
    decimal SoTienPhi,
    bool CoBangChungAnh,
    string TrangThai,
    DateTime NgayTao,
    DateTime? NgayXuLy,
    string? TenNguoiXuLy);

public sealed record AdminAccountAppealPageDto(
    IReadOnlyList<AdminAccountAppealRowDto> Items,
    string? TrangThai,
    string? LoaiYeuCau,
    string? TuKhoa,
    int Page,
    int TotalItems,
    int TotalPages,
    int PendingCount);

public sealed record AdminAccountAppealDetailDto(
    int MaKhieuNai,
    int MaNguoiDung,
    string HoTen,
    string Email,
    string? SoDienThoai,
    string TrangThaiTaiKhoan,
    int SoLanBiKhoaHienTai,
    string? LyDoKhoaHienTai,
    DateTime? ThoiGianKhoaHienTai,
    DateTime? MocTinhViPham,
    string? TenShop,
    string? TrangThaiShop,
    string LoaiYeuCau,
    int LanKhoaThu,
    string LyDoKhoaLucGui,
    string NoiDung,
    string? BangChungAnh,
    bool YeuCauPhi,
    decimal SoTienPhi,
    string? MaGiaoDichThamChieu,
    string TrangThai,
    string? GhiChuAdmin,
    int? NguoiXuLyId,
    string? TenNguoiXuLy,
    DateTime NgayTao,
    DateTime? NgayXuLy,
    int SoViPhamHieuLuc,
    int TongViPhamLichSu,
    IReadOnlyList<LockedViolationItemDto> ViPhamGanDay,
    IReadOnlyList<AccountAppealSummaryDto> CacDonKhacCuaUser);

public sealed record AdminAccountAppealActionResult(
    bool Success,
    string? Error,
    string? Message = null)
{
    public static AdminAccountAppealActionResult Ok(string message) => new(true, null, message);
    public static AdminAccountAppealActionResult Fail(string error) => new(false, error, null);
}
