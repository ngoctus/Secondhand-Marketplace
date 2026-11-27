namespace SecondHandMarketplace.Web.Services.Ai;

/// <summary>
/// Cấu hình hệ thống AI do Admin quản lý trong dbo.CauHinhAI:
/// - Cụm 1 (AI Hỗ trợ người mua &amp; người bán - dùng Ai:ApiKey): Chatbox, Trợ lý đăng bán (mô tả + giá), Model, System Prompt.
/// - Cụm 2 (AI Kiểm duyệt sản phẩm Tầng 2 - dùng Ai:ModerationApiKey): Bật/tắt, Model kiểm duyệt, số ảnh tối đa.
/// </summary>
/// <param name="IsPersisted">true nếu giá trị đang đọc từ database; false nếu là mặc định vì database chưa có bảng/dòng cấu hình.</param>
/// <param name="HasExtendedSchema">true nếu bảng CauHinhAI đã có đủ cột mở rộng (Trợ lý bán hàng &amp; Kiểm duyệt).</param>
public sealed record AiSettings(
    bool Enabled,
    string Model,
    string SystemPrompt,
    DateTime? UpdatedAtUtc,
    bool IsPersisted,
    bool SellerAssistantEnabled = true,
    bool ModerationEnabled = true,
    string? ModerationModel = null,
    int ModerationMaxImages = 2,
    bool HasExtendedSchema = true)
{
    public string EffectiveModerationModel =>
        string.IsNullOrWhiteSpace(ModerationModel) ? Model : ModerationModel;

    public static AiSettings FromDefaults(AiOptions options) =>
        new(
            Enabled: options.DefaultEnabled,
            Model: options.DefaultModel,
            SystemPrompt: options.DefaultSystemPrompt,
            UpdatedAtUtc: null,
            IsPersisted: false,
            SellerAssistantEnabled: options.DefaultEnabled,
            ModerationEnabled: options.ModerationEnabled,
            ModerationModel: options.ModerationModel,
            ModerationMaxImages: options.ModerationMaxImages,
            HasExtendedSchema: false);
}

/// <summary>Dữ liệu đầu vào khi Admin lưu cấu hình AI cho cả 2 cụm (Hỗ trợ người dùng/shop &amp; Kiểm duyệt).</summary>
public sealed record AiSettingsUpdate(
    bool Enabled,
    bool SellerAssistantEnabled,
    string Model,
    string SystemPrompt,
    bool ModerationEnabled,
    string ModerationModel,
    int ModerationMaxImages);
