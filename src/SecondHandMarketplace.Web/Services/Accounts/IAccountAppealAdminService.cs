namespace SecondHandMarketplace.Web.Services.Accounts;

/// <summary>
/// Hợp đồng tầng Admin cho Khiếu nại & Mở khóa tài khoản (Cài đặt bởi Người C trong KN-03).
/// </summary>
public interface IAccountAppealAdminService
{
    Task<AdminAccountAppealPageDto> GetAdminListAsync(
        AdminAccountAppealFilter filter,
        CancellationToken cancellationToken = default);

    Task<int> CountPendingAsync(CancellationToken cancellationToken = default);

    Task<AdminAccountAppealDetailDto?> GetAdminDetailAsync(
        int maKhieuNai,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Duyệt đơn & mở khóa tài khoản trong 1 transaction (reset MocTinhViPham; nếu LoaiYeuCau == APPEAL thì giảm SoLanBiKhoa - 1).
    /// </summary>
    Task<AdminAccountAppealActionResult> ApproveAsync(
        int adminId,
        int maKhieuNai,
        string? ghiChuAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Từ chối đơn (bắt buộc GhiChuAdmin >= 10 ký tự), giữ nguyên LOCKED và gửi email thông báo best-effort.
    /// </summary>
    Task<AdminAccountAppealActionResult> RejectAsync(
        int adminId,
        int maKhieuNai,
        string? ghiChuAdmin,
        CancellationToken cancellationToken = default);

    Task<AccountAppealSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);

    Task<AdminAccountAppealActionResult> UpdateSettingsAsync(
        int adminId,
        UpdateAccountAppealSettingsRequest request,
        CancellationToken cancellationToken = default);
}
