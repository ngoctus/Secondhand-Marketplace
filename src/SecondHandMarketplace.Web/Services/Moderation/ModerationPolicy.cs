namespace SecondHandMarketplace.Web.Services.Moderation;

/// <summary>
/// Hằng số nghiệp vụ C2 (HIGH/LOW). MỌI nơi dùng ngưỡng khóa hay các giá trị trạng thái phải lấy từ đây.
/// Giá trị chuỗi khớp CHECK constraint trong database/14_kiemduyet_tukhoa_c2.sql.
/// </summary>
public static class ModerationPolicy
{
    /// <summary>Số vi phạm (HIGH tự động + LOW đã được Admin xác nhận) để tự động khóa tài khoản người bán.</summary>
    public const int ViolationLockThreshold = 3;

    // Mức độ của từ khóa (TuKhoaCam.MucDo)
    public const string KeywordHigh = "HIGH";
    public const string KeywordLow = "LOW";

    // Mức cao nhất của một lần quét (PhanTichAI.MucDoTuKhoa)
    public const string LevelNone = "NONE";
    public const string LevelLow = "LOW";
    public const string LevelHigh = "HIGH";

    // PhanTichAI.TrangThai (giữ nguyên QUEUED / DONE / FAILED; "NONE" chỉ dùng khi đọc: lượt chưa có dòng phân tích)
    public const string AnalysisQueued = "QUEUED";
    public const string AnalysisDone = "DONE";
    public const string AnalysisFailed = "FAILED";
    public const string AnalysisMissing = "NONE";

    // LichSuViPham.NguonGhiNhan
    public const string SourceAutoHigh = "AUTO_HIGH";
    public const string SourceAdminRejectLow = "ADMIN_TU_CHOI_LOW";

    public const string ModelVersion = "keyword-v1";

    public const string ProductPending = "PENDING_REVIEW";
    public const string ProductRejected = "REJECTED";
}
