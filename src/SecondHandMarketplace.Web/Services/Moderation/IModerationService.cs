namespace SecondHandMarketplace.Web.Services.Moderation;

/// <summary>Kết quả đọc cho màn hình duyệt của C1.</summary>
public sealed record ModerationReview(
    int MaLuotKiemDuyet,
    int MaSanPham,
    int MaNguoiBan,
    bool TurnIsOpen,
    string AnalysisStatus,          // QUEUED | DONE | FAILED | NONE (chưa có dòng phân tích)
    string? Level,                  // NONE | LOW | HIGH | null (chưa quét từ khóa)
    IReadOnlyList<KeywordMatch> Matches,
    string? Explanation,
    string? ErrorMessage,
    int Attempts,
    bool ViolationRecorded,         // lượt này đã bị ghi nhận 1 vi phạm (HIGH tự động hoặc LOW bị Admin từ chối)
    int SellerViolationCount,
    int LockThreshold,
    bool CanApprove,                // false => C1 ẩn/vô hiệu nút Duyệt (DB cũng sẽ chặn bằng lỗi 50210/50211)
    string? ApproveBlockedReason,
    bool RejectionRecordsViolation, // true => nếu Admin TỪ CHỐI lượt này thì người bán bị ghi 1 vi phạm (LOW): C1 nên cảnh báo
    bool CanReanalyze);

/// <summary>Kết quả của AfterAdminDecisionAsync.</summary>
public sealed record AdminDecisionFollowUp(
    bool ViolationRecorded,         // lượt vừa quyết định có 1 vi phạm trong LichSuViPham (do trigger file 17 ghi khi từ chối LOW)
    int ViolationCount,
    bool SellerLockedNow,           // true = lần gọi này đã khóa người bán vì đạt ngưỡng
    bool SellerLocked);

/// <summary>
/// Moderation (C2): quét từ khóa cấm HIGH/LOW, lưu PhanTichAI, ghi nhận vi phạm, tự khóa khi đạt ngưỡng.
/// Hợp đồng với nơi gọi (giống INotificationService): service dùng chung AppDbContext (scoped), tự mở transaction cho phần
/// ghi của nó, và sẽ lưu mọi thay đổi chưa lưu của context -> hãy SaveChanges nghiệp vụ chính xong rồi mới gọi.
/// </summary>
public interface IModerationService
{
    /// <summary>
    /// B gọi NGAY SAU khi sp_GuiDuyetSanPham thành công. Cũng dùng để phân tích lại (kể cả FAILED) cho lượt đang chờ của sản phẩm.
    /// - Quét tên + mô tả; cập nhật dòng PhanTichAI của lượt đang chờ (tạo mới nếu chưa có, không bao giờ tạo trùng).
    /// - HIGH: tự từ chối (quyết định hệ thống, không giả Admin) + ghi nhận 1 vi phạm. LOW: chờ Admin (duyệt = không vi phạm, từ chối = 1 vi phạm). Không có: chờ Admin duyệt như thường.
    /// - Lỗi khi quét: ghi FAILED (không tính vi phạm), không ném exception; phân tích lại sau được.
    /// - Sản phẩm không còn ở trạng thái chờ duyệt: không làm gì.
    /// </summary>
    Task PhanTichSanPhamAsync(int maSanPham, CancellationToken cancellationToken = default);

    /// <summary>Kết quả phân tích của lượt đang chờ của sản phẩm (null nếu không có lượt đang chờ).</summary>
    Task<ModerationReview?> GetReviewAsync(int maSanPham, CancellationToken cancellationToken = default);

    /// <summary>Kết quả phân tích của một lượt bất kỳ (xem lịch sử).</summary>
    Task<ModerationReview?> GetReviewByTurnAsync(int maLuotKiemDuyet, CancellationToken cancellationToken = default);

    /// <summary>
    /// C1 gọi SAU KHI Admin duyệt hoặc từ chối sản phẩm (sau khi sp_DuyetSanPham/sp_TuChoiSanPham thành công).
    /// - Vi phạm LOW khi từ chối đã được ghi trong CÙNG transaction của quyết định (trigger, database/17_...sql); hàm này
    ///   chỉ đọc lại và, nếu người bán đạt ngưỡng, khóa tài khoản qua IUserAdminService.LockUserAsync + thông báo.
    /// - Gọi lặp hoặc đồng thời an toàn: không ghi thêm vi phạm, không khóa/thông báo lần hai.
    /// - Duyệt: không vi phạm nào được ghi; hàm trả ViolationRecorded = false.
    /// Trả null nếu sản phẩm chưa có lượt nào đã có quyết định.
    /// </summary>
    Task<AdminDecisionFollowUp?> AfterAdminDecisionAsync(int maSanPham, CancellationToken cancellationToken = default);

    /// <summary>Số vi phạm hiện có của người bán (bộ đếm chung HIGH + LOW đã xác nhận).</summary>
    Task<int> GetViolationCountAsync(int sellerUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra lại ngưỡng và khóa nếu đạt (idempotent). Dùng để vá trường hợp tiến trình chết giữa lúc ghi vi phạm và lúc khóa.
    /// Trả true nếu lần gọi này đã khóa người bán.
    /// </summary>
    Task<bool> EnforceViolationThresholdAsync(int sellerUserId, CancellationToken cancellationToken = default);
}
