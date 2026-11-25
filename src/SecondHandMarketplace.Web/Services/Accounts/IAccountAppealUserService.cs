namespace SecondHandMarketplace.Web.Services.Accounts;

/// <summary>
/// Hợp đồng tầng User cho trang /Account/Locked và gửi yêu cầu mở khóa/khiếu nại (Cài đặt bởi Người D trong KN-06).
/// </summary>
public interface IAccountAppealUserService
{
    /// <summary>
    /// Tổng hợp thông tin trạng thái khóa, lịch sử gậy hiệu lực, số lượt còn lại / Cooldown và mã VietQR (nếu khóa lần 2).
    /// Trả về null nếu không tìm thấy người dùng.
    /// </summary>
    Task<LockedAccountOverviewDto?> GetLockedAccountOverviewAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gửi đơn mới (APPEAL hoặc UNLOCK_REQUEST) sau khi kiểm tra trạng thái LOCKED, lần khóa, ảnh biên lai lần 2, đơn PENDING và Cooldown.
    /// </summary>
    Task<SubmitAccountAppealResult> SubmitAppealAsync(
        int userId,
        string loaiYeuCau,
        string noiDung,
        string? bangChungAnh,
        CancellationToken cancellationToken = default);
}
