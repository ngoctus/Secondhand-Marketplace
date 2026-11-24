using SecondHandMarketplace.Web.Services.Moderation;

namespace SecondHandMarketplace.Web.Services.ProductReview;

/// <summary>Nhãn hiển thị cho kết quả phân tích C2 (dùng chung cho danh sách và chi tiết).</summary>
public static class ReviewDisplay
{
    public static (string Text, string BadgeClass) Describe(string? analysisStatus, string? level) =>
        (analysisStatus, level) switch
        {
            (null, _) or (ModerationPolicy.AnalysisMissing, _) => ("Chưa có phân tích", "bg-secondary"),
            (ModerationPolicy.AnalysisQueued, _) => ("Đang chờ phân tích", "bg-warning text-dark"),
            (ModerationPolicy.AnalysisFailed, _) => ("Phân tích thất bại", "bg-danger"),
            (ModerationPolicy.AnalysisDone, null) => ("Chưa quét từ khóa (cần phân tích lại)", "bg-warning text-dark"),
            (ModerationPolicy.AnalysisDone, ModerationPolicy.LevelNone) => ("Không có cảnh báo từ khóa", "bg-success"),
            (ModerationPolicy.AnalysisDone, ModerationPolicy.LevelLow) => ("Cảnh báo từ khóa LOW", "bg-warning text-dark"),
            (ModerationPolicy.AnalysisDone, ModerationPolicy.LevelHigh) => ("Cảnh báo từ khóa HIGH", "bg-danger"),
            _ => (analysisStatus ?? "", "bg-secondary")
        };

    public static (string Text, string BadgeClass) DescribeLane(
        string? analysisStatus,
        string? keywordLevel,
        string? riskLevel,
        string? recommendation,
        string? modelVersion)
    {
        if (analysisStatus != ModerationPolicy.AnalysisDone || keywordLevel is null)
            return Describe(analysisStatus, keywordLevel);

        if (keywordLevel == ModerationPolicy.LevelHigh)
            return ("Làn Đỏ — Tự từ chối HIGH", "bg-danger");

        if (ModerationPolicy.IsGreenLane(analysisStatus, keywordLevel, riskLevel, recommendation, modelVersion))
            return ("Làn Xanh — An toàn", "bg-success");

        if (ModerationPolicy.IsFallbackModel(modelVersion))
            return ("Làn Vàng — Fallback từ khóa", "bg-warning text-dark");

        if (riskLevel == ModerationPolicy.RiskHigh || recommendation == ModerationPolicy.AdviceReject)
            return ("Làn Vàng — AI cảnh báo cao", "bg-danger");

        return ("Làn Vàng — Cần xem kỹ", "bg-warning text-dark");
    }

    public static (string Text, string BadgeClass) DescribeRisk(string? riskLevel) => riskLevel switch
    {
        ModerationPolicy.RiskLow => ("Rủi ro thấp (LOW)", "bg-success"),
        ModerationPolicy.RiskMedium => ("Rủi ro trung bình (MEDIUM)", "bg-warning text-dark"),
        ModerationPolicy.RiskHigh => ("Rủi ro cao (HIGH)", "bg-danger"),
        _ => ("Chưa đánh giá", "bg-secondary")
    };

    public static (string Text, string BadgeClass) DescribeRecommendation(string? recommendation) => recommendation switch
    {
        ModerationPolicy.AdviceApprove => ("AI đề xuất: Duyệt", "bg-success"),
        ModerationPolicy.AdviceNeedsReview => ("AI đề xuất: Cần xem kỹ", "bg-warning text-dark"),
        ModerationPolicy.AdviceReject => ("AI đề xuất: Nên từ chối", "bg-danger"),
        _ => ("Chưa có đề xuất", "bg-secondary")
    };

    public static string DescribeAiSignalGroup(string? group) => group?.Trim().ToUpperInvariant() switch
    {
        "LACH_LUAT" => "Lách luật từ ngữ",
        "NGOAI_SAN" => "Kéo giao dịch ngoài sàn",
        "BAT_THUONG_GIA" => "Bất thường giá / mô tả",
        "ANH_KHONG_KHOP" => "Ảnh không khớp / nhạy cảm",
        "HANG_CAM" => "Nghi ngờ hàng cấm",
        "SPAM_KHAC" => "Spam / Rủi ro khác",
        _ => group ?? "Khác"
    };

    public static string DescribeField(string? field) => field switch
    {
        "Ten" => "Tên sản phẩm",
        "MoTa" => "Mô tả",
        "Gia" => "Giá bán",
        "Anh" => "Ảnh đại diện",
        _ => field ?? ""
    };

    public static string StatusText(string? productStatus) => productStatus switch
    {
        "PENDING_REVIEW" => "Chờ duyệt",
        "ACTIVE" => "Đang bán",
        "REJECTED" => "Bị từ chối",
        "REMOVED" => "Đã gỡ",
        "HIDDEN" => "Đang ẩn",
        "RESERVED" => "Đã giữ chỗ",
        "SOLD" => "Đã bán",
        "DRAFT" => "Nháp",
        _ => productStatus ?? ""
    };
}
