namespace SecondHandMarketplace.Web.Services.Ai;

public enum AiChatStatus
{
    Success,
    InvalidInput,
    Forbidden,
    Disabled,
    NotConfigured,
    Timeout,
    ProviderError,
    InvalidResponse
}

/// <summary>Trạng thái bật/tắt của nhóm AI hỗ trợ người mua &amp; người bán.</summary>
public sealed record AiFeatureStatus(bool ChatEnabled, bool SellerAssistantEnabled);

/// <summary>Đầu vào cho endpoint tách riêng hỗ trợ viết mô tả sản phẩm (POST /api/ai/draft-description).</summary>
public sealed record AiDescriptionDraftInput(
    string? Name,
    int? CategoryId = null,
    int? ConditionId = null,
    string? CategoryName = null,
    string? ConditionName = null,
    decimal? Price = null,
    string? CurrentDescription = null,
    string? Language = null);

/// <summary>
/// Kết quả một lượt chat / soạn mô tả. Error luôn là câu an toàn để hiển thị cho người dùng
/// (không chứa khóa API, URL nội bộ hay chi tiết lỗi của nhà cung cấp).
/// </summary>
public sealed record AiChatResult(AiChatStatus Status, string? Reply = null, string? Error = null)
{
    public bool IsSuccess => Status == AiChatStatus.Success;

    public static AiChatResult Ok(string reply) => new(AiChatStatus.Success, reply);
    public static AiChatResult Fail(AiChatStatus status, string error) => new(status, null, error);
}
