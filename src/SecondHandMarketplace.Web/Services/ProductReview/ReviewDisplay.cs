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
            (ModerationPolicy.AnalysisDone, ModerationPolicy.LevelNone) => ("Không có cảnh báo", "bg-success"),
            (ModerationPolicy.AnalysisDone, ModerationPolicy.LevelLow) => ("Cảnh báo LOW", "bg-warning text-dark"),
            (ModerationPolicy.AnalysisDone, ModerationPolicy.LevelHigh) => ("Cảnh báo HIGH", "bg-danger"),
            _ => (analysisStatus ?? "", "bg-secondary")
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
