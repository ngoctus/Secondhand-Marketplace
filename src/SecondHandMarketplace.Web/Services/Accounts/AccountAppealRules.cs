namespace SecondHandMarketplace.Web.Services.Accounts;

/// <summary>
/// Hằng số và hàm quy tắc thuần dùng chung giữa luồng Admin (KN-03) và luồng User (KN-06)
/// cho chức năng Khiếu nại khóa nhầm / Cam kết mở khóa tài khoản.
/// Giá trị chuỗi khớp với CHECK constraint trong bảng KhieuNaiTaiKhoan.
/// </summary>
public static class AccountAppealRules
{
    public const int ConfigRowId = 1;

    // LoaiYeuCau
    public const string TypeAppeal = "APPEAL";
    public const string TypeUnlockRequest = "UNLOCK_REQUEST";

    // TrangThai
    public const string StatusPending = "PENDING";
    public const string StatusApproved = "APPROVED";
    public const string StatusRejected = "REJECTED";

    // Ngưỡng lần khóa
    public const int FeeLockTier = 2;
    public const int PermanentLockThreshold = 3;

    // Độ dài nội dung / ghi chú
    public const int MinContentLength = 20;
    public const int MaxContentLength = 2000;
    public const int MinAdminRejectNoteLength = 10;
    public const int MaxAdminNoteLength = 1000;
    public const int MaxEvidencePathLength = 500;

    // Phân trang
    public const int PageSize = 15;

    // Giá trị mặc định khi bảng CauHinhKhieuNaiTaiKhoan chưa có dữ liệu (InMemory test / DB mới)
    public const int DefaultMaxAttemptsPerCycle = 2;
    public const int DefaultCooldownMinutes = 4320; // 3 ngày
    public const decimal DefaultSecondLockFee = 200_000m;
    public const string DefaultBankName = "MB Bank";
    public const string DefaultBankCode = "MB";
    public const string DefaultBankAccount = "090123456789";
    public const string DefaultBankOwner = "RE MARKET PLATFORM";

    public static bool IsValidRequestType(string? loaiYeuCau) =>
        string.Equals(loaiYeuCau, TypeAppeal, StringComparison.OrdinalIgnoreCase)
        || string.Equals(loaiYeuCau, TypeUnlockRequest, StringComparison.OrdinalIgnoreCase);

    public static string NormalizeRequestType(string? loaiYeuCau) =>
        string.Equals(loaiYeuCau?.Trim(), TypeUnlockRequest, StringComparison.OrdinalIgnoreCase)
            ? TypeUnlockRequest
            : TypeAppeal;

    /// <summary>
    /// Tài khoản đã LOCKED từ seed cũ có thể mang SoLanBiKhoa = 0; chuẩn hóa về tối thiểu 1 khi đang khóa.
    /// </summary>
    public static int NormalizeLockCount(int soLanBiKhoa, bool isLocked) =>
        isLocked ? Math.Max(1, soLanBiKhoa) : Math.Max(0, soLanBiKhoa);

    /// <summary>Cam kết mở khóa chỉ mở cho lần khóa 1 và 2; từ lần 3 trở đi chỉ cho phép khiếu nại khóa nhầm.</summary>
    public static bool IsUnlockRequestAllowed(int effectiveLockCount) =>
        effectiveLockCount < PermanentLockThreshold;

    /// <summary>Phí xử lý 200.000đ + bắt buộc ảnh biên lai chỉ áp dụng cho Cam kết mở khóa ở lần khóa thứ 2.</summary>
    public static bool RequiresUnlockFee(string loaiYeuCau, int effectiveLockCount) =>
        string.Equals(loaiYeuCau, TypeUnlockRequest, StringComparison.OrdinalIgnoreCase)
        && effectiveLockCount == FeeLockTier;

    /// <summary>Nội dung chuyển khoản VietQR chuẩn, tối đa 25 ký tự không dấu.</summary>
    public static string BuildTransferContent(int userId, int effectiveLockCount) =>
        $"MOKHOA U{userId} L{Math.Max(1, effectiveLockCount)}";

    /// <summary>
    /// Đánh giá chu kỳ gửi và trạng thái Cooldown dựa trên danh sách thời điểm xử lý (UTC, tăng dần)
    /// của các đơn bị REJECTED trong đợt khóa hiện tại.
    /// </summary>
    public static AppealCycleEvaluation EvaluateCycle(
        IReadOnlyList<DateTime> rejectedProcessedUtcAsc,
        int maxAttemptsPerCycle,
        int cooldownMinutes,
        DateTime utcNow)
    {
        var limit = Math.Max(1, maxAttemptsPerCycle);
        var cooldown = Math.Max(1, cooldownMinutes);
        var count = rejectedProcessedUtcAsc.Count;

        if (count == 0)
            return new AppealCycleEvaluation(0, limit, false, null);

        var remainder = count % limit;
        if (remainder > 0)
            return new AppealCycleEvaluation(remainder, limit - remainder, false, null);

        // Đã bị từ chối đủ bội số của chu kỳ (2, 4, 6...): kiểm tra thời gian chờ từ đơn từ chối cuối cùng.
        var lastRejectedAt = rejectedProcessedUtcAsc[^1];
        var cooldownEndsAt = lastRejectedAt.AddMinutes(cooldown);
        if (utcNow < cooldownEndsAt)
            return new AppealCycleEvaluation(limit, 0, true, cooldownEndsAt);

        // Đã qua Cooldown: mở chu kỳ gửi mới.
        return new AppealCycleEvaluation(0, limit, false, null);
    }

    public static (int Days, int Hours, int Minutes) SplitCooldownMinutes(int totalMinutes)
    {
        var safe = Math.Max(1, totalMinutes);
        var days = safe / (24 * 60);
        var rem = safe % (24 * 60);
        var hours = rem / 60;
        var minutes = rem % 60;
        return (days, hours, minutes);
    }

    public static int CombineCooldownMinutes(int days, int hours, int minutes)
    {
        var total = (long)Math.Max(0, days) * 24 * 60
                    + (long)Math.Max(0, hours) * 60
                    + Math.Max(0, minutes);
        if (total > int.MaxValue) return int.MaxValue;
        return (int)total;
    }

    public static string FormatCooldownDuration(int totalMinutes)
    {
        var (d, h, m) = SplitCooldownMinutes(totalMinutes);
        var parts = new List<string>(3);
        if (d > 0) parts.Add($"{d} ngày");
        if (h > 0) parts.Add($"{h} giờ");
        if (m > 0 || parts.Count == 0) parts.Add($"{m} phút");
        return string.Join(" ", parts);
    }

    public static string FormatRemainingTime(TimeSpan remaining)
    {
        if (remaining <= TimeSpan.Zero) return "0 phút";
        if (remaining.TotalMinutes < 1) return "dưới 1 phút";
        var totalMinutes = (int)Math.Ceiling(remaining.TotalMinutes);
        return FormatCooldownDuration(totalMinutes);
    }

    public static string RequestTypeLabel(string loaiYeuCau, int lockCount = 1) => loaiYeuCau switch
    {
        TypeAppeal when lockCount >= PermanentLockThreshold => Common.HienThi.Chon("Khiếu nại khóa nhầm / Xem xét đặc biệt", "Wrongful Lock Appeal / Special Review"),
        TypeAppeal => Common.HienThi.Chon("Khiếu nại khóa nhầm", "Wrongful Lock Appeal"),
        TypeUnlockRequest => Common.HienThi.Chon("Cam kết mở khóa", "Unlock Commitment"),
        _ => loaiYeuCau
    };

    public static string StatusLabel(string trangThai) => trangThai switch
    {
        StatusPending => Common.HienThi.Chon("Đang chờ duyệt", "Pending Review"),
        StatusApproved => Common.HienThi.Chon("Đã chấp thuận", "Approved"),
        StatusRejected => Common.HienThi.Chon("Đã từ chối", "Rejected"),
        _ => trangThai
    };
}

public sealed record AppealCycleEvaluation(
    int AttemptsUsedInCurrentCycle,
    int AttemptsRemainingInCurrentCycle,
    bool IsInCooldown,
    DateTime? CooldownEndsAtUtc);
