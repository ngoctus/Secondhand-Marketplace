namespace SecondHandMarketplace.Web.Services.Ai;

/// <summary>Đầu vào cho chức năng AI tham khảo khoảng giá bán (C-03).</summary>
public sealed record AiPriceSuggestInput(
    string? Name,
    int? CategoryId,
    int? ConditionId,
    string? CategoryName = null,
    string? ConditionName = null,
    string? Description = null,
    string? Language = null);

/// <summary>Sản phẩm thật trên sàn được dùng làm mẫu đối chiếu giá.</summary>
public sealed record AiPriceSample(
    int Id,
    string Name,
    decimal PriceVnd,
    string? Condition,
    string Url);

/// <summary>
/// Dữ liệu gợi ý khoảng giá trả về cho client:
/// thống kê Min/Median/Max tính bằng C# từ dữ liệu thật trên sàn, kèm giá đề xuất (bội 1.000đ) và giải thích từ AI.
/// </summary>
public sealed record AiPriceSuggestionData(
    decimal? MinPriceVnd,
    decimal? MaxPriceVnd,
    decimal? MedianPriceVnd,
    decimal? RecommendedPriceVnd,
    int SampleCount,
    string MatchScope,
    string Explanation,
    bool UsedAi,
    IReadOnlyList<AiPriceSample> Samples);

/// <summary>Kết quả nghiệp vụ của chức năng gợi ý giá.</summary>
public sealed record AiPriceSuggestionResult(
    AiChatStatus Status,
    AiPriceSuggestionData? Data = null,
    string? Error = null)
{
    public bool IsSuccess => Status == AiChatStatus.Success && Data is not null;

    public static AiPriceSuggestionResult Ok(AiPriceSuggestionData data) => new(AiChatStatus.Success, data);
    public static AiPriceSuggestionResult Fail(AiChatStatus status, string error) => new(status, null, error);
}
