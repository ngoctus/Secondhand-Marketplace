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

    // Mức rủi ro tổng hợp (PhanTichAI.MucRuiRo)
    public const string RiskLow = "LOW";
    public const string RiskMedium = "MEDIUM";
    public const string RiskHigh = "HIGH";

    // Đề xuất xử lý (PhanTichAI.DeXuat)
    public const string AdviceApprove = "APPROVE";
    public const string AdviceNeedsReview = "NEEDS_REVIEW";
    public const string AdviceReject = "REJECT";

    // Loại phần tử trong JSON PhanTichAI.DauHieu
    public const string EvidenceTypeKeyword = "TU_KHOA_CAM";
    public const string EvidenceTypeAi = "AI_CANH_BAO";

    // LichSuViPham.NguonGhiNhan
    public const string SourceAutoHigh = "AUTO_HIGH";
    public const string SourceAdminRejectLow = "ADMIN_TU_CHOI_LOW";

    public const string ModelVersion = "keyword-v1";
    public const string ModelVersionFallback = "keyword-v1-fallback";
    public const string HybridModelPrefix = "hybrid:";
    public const int MaxModelVersionLength = 50;

    public const string ProductPending = "PENDING_REVIEW";
    public const string ProductRejected = "REJECTED";

    public static bool IsHybridModel(string? modelVersion) =>
        !string.IsNullOrWhiteSpace(modelVersion)
        && modelVersion.StartsWith(HybridModelPrefix, StringComparison.OrdinalIgnoreCase);

    public static bool IsFallbackModel(string? modelVersion) =>
        string.Equals(modelVersion, ModelVersionFallback, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Làn Xanh (đủ điều kiện duyệt nhanh):
    /// Đã phân tích DONE + Tầng 1 NONE + Tầng 2 AI thật (hybrid:...) đánh giá rủi ro LOW và đề xuất APPROVE.
    /// Bản ghi fallback (keyword-v1-fallback) hoặc chỉ chạy từ khóa (keyword-v1) KHÔNG thuộc Làn Xanh.
    /// </summary>
    public static bool IsGreenLane(
        string? analysisStatus,
        string? keywordLevel,
        string? riskLevel,
        string? recommendation,
        string? modelVersion) =>
        analysisStatus == AnalysisDone
        && keywordLevel == LevelNone
        && riskLevel == RiskLow
        && recommendation == AdviceApprove
        && IsHybridModel(modelVersion);
}
