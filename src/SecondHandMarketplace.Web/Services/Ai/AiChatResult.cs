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

/// <summary>
/// Kết quả một lượt chat. Error luôn là câu tiếng Việt an toàn để hiển thị cho người dùng
/// (không chứa khóa API, URL nội bộ hay chi tiết lỗi của nhà cung cấp).
/// </summary>
public sealed record AiChatResult(AiChatStatus Status, string? Reply = null, string? Error = null)
{
    public bool IsSuccess => Status == AiChatStatus.Success;

    public static AiChatResult Ok(string reply) => new(AiChatStatus.Success, reply);
    public static AiChatResult Fail(AiChatStatus status, string error) => new(status, null, error);
}
