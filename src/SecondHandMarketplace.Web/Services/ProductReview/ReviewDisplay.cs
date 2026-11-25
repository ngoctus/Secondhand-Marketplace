using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Services.Moderation;

namespace SecondHandMarketplace.Web.Services.ProductReview;

/// <summary>Nhãn hiển thị đa ngôn ngữ (VI | EN) cho kết quả phân tích C-01/C-02 (dùng chung cho danh sách và chi tiết).</summary>
public static class ReviewDisplay
{
    public static (string Text, string BadgeClass) Describe(string? analysisStatus, string? level, bool forceVietnamese = false)
    {
        string Pick(string vi, string en) => forceVietnamese ? vi : HienThi.Chon(vi, en);
        return (analysisStatus, level) switch
        {
            (null, _) or (ModerationPolicy.AnalysisMissing, _) =>
                (Pick("Chưa có phân tích", "Not analyzed"), "bg-secondary"),
            (ModerationPolicy.AnalysisQueued, _) =>
                (Pick("Đang chờ phân tích", "Queued for analysis"), "bg-warning text-dark"),
            (ModerationPolicy.AnalysisFailed, _) =>
                (Pick("Phân tích thất bại", "Analysis failed"), "bg-danger"),
            (ModerationPolicy.AnalysisDone, null) =>
                (Pick("Chưa quét từ khóa (cần phân tích lại)", "Not keyword-scanned (reanalyze needed)"), "bg-warning text-dark"),
            (ModerationPolicy.AnalysisDone, ModerationPolicy.LevelNone) =>
                (Pick("Không có cảnh báo từ khóa", "No keyword warnings"), "bg-success"),
            (ModerationPolicy.AnalysisDone, ModerationPolicy.LevelLow) =>
                (Pick("Cảnh báo từ khóa LOW", "LOW keyword warning"), "bg-warning text-dark"),
            (ModerationPolicy.AnalysisDone, ModerationPolicy.LevelHigh) =>
                (Pick("Cảnh báo từ khóa HIGH", "HIGH keyword warning"), "bg-danger"),
            _ => (analysisStatus ?? "", "bg-secondary")
        };
    }

    public static (string Text, string BadgeClass) DescribeLane(
        string? analysisStatus,
        string? keywordLevel,
        string? riskLevel,
        string? recommendation,
        string? modelVersion,
        bool forceVietnamese = false)
    {
        string Pick(string vi, string en) => forceVietnamese ? vi : HienThi.Chon(vi, en);

        if (analysisStatus != ModerationPolicy.AnalysisDone || keywordLevel is null)
            return Describe(analysisStatus, keywordLevel, forceVietnamese);

        if (keywordLevel == ModerationPolicy.LevelHigh)
            return (Pick("Làn Đỏ — Tự từ chối HIGH", "Red Lane — Auto-Rejected (HIGH)"), "bg-danger");

        if (ModerationPolicy.IsGreenLane(analysisStatus, keywordLevel, riskLevel, recommendation, modelVersion))
            return (Pick("Làn Xanh — An toàn", "Green Lane — Safe"), "bg-success");

        if (ModerationPolicy.IsFallbackModel(modelVersion))
            return (Pick("Làn Vàng — Fallback từ khóa", "Yellow Lane — Keyword Fallback"), "bg-warning text-dark");

        if (riskLevel == ModerationPolicy.RiskHigh || recommendation == ModerationPolicy.AdviceReject)
            return (Pick("Làn Vàng — AI cảnh báo cao", "Yellow Lane — High AI Alert"), "bg-danger");

        return (Pick("Làn Vàng — Cần xem kỹ", "Yellow Lane — Needs Review"), "bg-warning text-dark");
    }

    public static string? DescribeApproveBlocked(ModerationReview? review)
    {
        if (review is null) return null;
        if (!review.TurnIsOpen)
            return HienThi.Chon("Lượt kiểm duyệt này đã có quyết định.", "This moderation turn has already been decided.");
        if (review.AnalysisStatus != ModerationPolicy.AnalysisDone || review.Level is null)
            return HienThi.Chon("Sản phẩm chưa được phân tích từ khóa xong (QUEUED/FAILED hoặc chưa quét).", "Product analysis is not complete yet (QUEUED/FAILED or not scanned).");
        if (review.Level == ModerationPolicy.LevelHigh || review.ViolationRecorded)
            return HienThi.Chon("Sản phẩm có từ khóa cấm mức HIGH hoặc đã bị ghi nhận vi phạm.", "Product contains HIGH forbidden keywords or already has a recorded violation.");
        return null;
    }

    public static (string Text, string BadgeClass) DescribeRisk(string? riskLevel) => riskLevel switch
    {
        ModerationPolicy.RiskLow => (HienThi.Chon("Rủi ro thấp (LOW)", "Low Risk (LOW)"), "bg-success"),
        ModerationPolicy.RiskMedium => (HienThi.Chon("Rủi ro trung bình (MEDIUM)", "Medium Risk (MEDIUM)"), "bg-warning text-dark"),
        ModerationPolicy.RiskHigh => (HienThi.Chon("Rủi ro cao (HIGH)", "High Risk (HIGH)"), "bg-danger"),
        _ => (HienThi.Chon("Chưa đánh giá", "Not assessed"), "bg-secondary")
    };

    public static (string Text, string BadgeClass) DescribeRecommendation(string? recommendation) => recommendation switch
    {
        ModerationPolicy.AdviceApprove => (HienThi.Chon("AI đề xuất: Duyệt", "AI Suggests: Approve"), "bg-success"),
        ModerationPolicy.AdviceNeedsReview => (HienThi.Chon("AI đề xuất: Cần xem kỹ", "AI Suggests: Needs Review"), "bg-warning text-dark"),
        ModerationPolicy.AdviceReject => (HienThi.Chon("AI đề xuất: Nên từ chối", "AI Suggests: Reject"), "bg-danger"),
        _ => (HienThi.Chon("Chưa có đề xuất", "No recommendation"), "bg-secondary")
    };

    public static string DescribeAiSignalGroup(string? group) => group?.Trim().ToUpperInvariant() switch
    {
        "LACH_LUAT" => HienThi.Chon("Lách luật từ ngữ", "Keyword evasion"),
        "NGOAI_SAN" => HienThi.Chon("Kéo giao dịch ngoài sàn", "Off-platform contact"),
        "BAT_THUONG_GIA" => HienThi.Chon("Bất thường giá / mô tả", "Abnormal price / description"),
        "ANH_KHONG_KHOP" => HienThi.Chon("Ảnh không khớp / nhạy cảm", "Mismatched / sensitive image"),
        "HANG_CAM" => HienThi.Chon("Nghi ngờ hàng cấm", "Suspected prohibited item"),
        "SPAM_KHAC" => HienThi.Chon("Spam / Rủi ro khác", "Spam / Other risk"),
        _ => group ?? HienThi.Chon("Khác", "Other")
    };

    public static string DescribeField(string? field) => field switch
    {
        "Ten" => HienThi.Chon("Tên sản phẩm", "Product name"),
        "MoTa" => HienThi.Chon("Mô tả", "Description"),
        "Gia" => HienThi.Chon("Giá bán", "Price"),
        "Anh" => HienThi.Chon("Ảnh đại diện", "Cover image"),
        _ => field ?? ""
    };

    public static string DescribeDecision(string? decision) => decision switch
    {
        "APPROVED" => HienThi.Chon("Đã duyệt", "Approved"),
        "REJECTED" => HienThi.Chon("Từ chối", "Rejected"),
        null or "" => HienThi.Chon("Đang chờ", "Pending"),
        _ => decision
    };

    public static string StatusText(string? productStatus) => HienThi.TrangThaiSanPham(productStatus);
}
