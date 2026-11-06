namespace SecondHandMarketplace.Web.Services.Ai;

/// <summary>Cấu hình Chatbox do Admin quản lý (bật/tắt, model, System Prompt).</summary>
/// <param name="IsPersisted">true nếu giá trị đang đọc từ database; false nếu là mặc định vì database chưa có bảng/dòng cấu hình.</param>
public sealed record AiSettings(bool Enabled, string Model, string SystemPrompt, DateTime? UpdatedAtUtc, bool IsPersisted)
{
    public static AiSettings FromDefaults(AiOptions options) =>
        new(options.DefaultEnabled, options.DefaultModel, options.DefaultSystemPrompt, null, false);
}
