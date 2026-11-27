namespace SecondHandMarketplace.Web.Models.ViewModels.Ai;

/// <summary>Yêu cầu AI soạn/trau chuốt mô tả sản phẩm từ form đăng/sửa tin (POST /api/ai/draft-description).</summary>
public record AiDescriptionDraftRequest(
    string? Name,
    int? CategoryId = null,
    int? ConditionId = null,
    string? CategoryName = null,
    string? ConditionName = null,
    decimal? Price = null,
    string? CurrentDescription = null,
    string? Language = null);
