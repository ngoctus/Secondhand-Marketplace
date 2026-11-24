namespace SecondHandMarketplace.Web.Models.ViewModels.Ai;

/// <summary>Yêu cầu gợi ý khoảng giá bán từ form đăng/sửa sản phẩm (C-03).</summary>
public record AiPriceSuggestRequest(
    string? Name,
    int? CategoryId,
    int? ConditionId,
    string? CategoryName = null,
    string? ConditionName = null,
    string? Description = null,
    string? Language = null);
