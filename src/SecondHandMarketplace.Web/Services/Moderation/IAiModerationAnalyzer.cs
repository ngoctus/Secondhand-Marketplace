namespace SecondHandMarketplace.Web.Services.Moderation;

/// <summary>Ảnh đại diện sản phẩm mã hóa Base64 gửi kèm Tầng 2 LLM.</summary>
public sealed record AiModerationImage(string MimeType, string Base64Data);

/// <summary>Ngữ cảnh đầy đủ gửi lên Tầng 2 LLM khi Tầng 1 trả về NONE hoặc LOW.</summary>
public sealed record AiModerationInput(
    int ProductId,
    string Name,
    string? Description,
    decimal? Price,
    string? CategoryName,
    string? ConditionName,
    string KeywordLevel,
    IReadOnlyList<KeywordMatch> KeywordMatches,
    IReadOnlyList<AiModerationImage> Images);

/// <summary>Một dấu hiệu rủi ro do AI Tầng 2 phát hiện (lưu trong JSON PhanTichAI.DauHieu với loai = "AI_CANH_BAO").</summary>
public sealed record AiModerationSignal(
    string Group,
    string Level,
    string Field,
    string Evidence);

/// <summary>Kết quả phân tích ngữ cảnh + ảnh từ Tầng 2 LLM.</summary>
public sealed record AiModerationResult(
    bool Succeeded,
    string RiskLevel,
    string Recommendation,
    string Explanation,
    IReadOnlyList<AiModerationSignal> Signals,
    string ModelName,
    string? ErrorMessage = null)
{
    public static AiModerationResult Ok(
        string riskLevel,
        string recommendation,
        string explanation,
        IReadOnlyList<AiModerationSignal> signals,
        string modelName) =>
        new(true, riskLevel, recommendation, explanation, signals, modelName);

    public static AiModerationResult Fail(string errorMessage, string modelName = "") =>
        new(false, ModerationPolicy.RiskLow, ModerationPolicy.AdviceNeedsReview, "", Array.Empty<AiModerationSignal>(), modelName, errorMessage);
}

/// <summary>
/// Tầng 2 (C-01): phân tích ngữ cảnh, lách luật, bất thường giá và ảnh đại diện bằng LLM.
/// Không bao giờ ném lỗi hạ tầng ra ngoài trừ OperationCanceledException khi caller chủ động hủy.
/// </summary>
public interface IAiModerationAnalyzer
{
    Task<AiModerationResult> AnalyzeAsync(AiModerationInput input, CancellationToken cancellationToken = default);
}
