namespace SecondHandMarketplace.Web.Services.Ai;

/// <summary>Đọc/ghi cấu hình hệ thống AI (Hỗ trợ người mua &amp; người bán + AI Kiểm duyệt) trong database.</summary>
public interface IAiSettingsService
{
    /// <summary>Danh sách model Admin được phép chọn.</summary>
    IReadOnlyList<string> AllowedModels { get; }

    /// <summary>
    /// Đọc cấu hình hiện hành TỪ DATABASE ở mỗi lần gọi (không cache) để thay đổi của Admin có hiệu lực ngay.
    /// Khi bảng/dòng cấu hình chưa tồn tại, trả về mặc định (IsPersisted = false) thay vì ném lỗi.
    /// </summary>
    Task<AiSettings> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Kiểm tra và lưu cấu hình đầy đủ cho cả 2 cụm AI (Hỗ trợ người mua/bán &amp; Kiểm duyệt).</summary>
    Task<(bool Success, string? Error)> SaveAsync(AiSettingsUpdate update, int? adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Quá tải tương thích ngược: chỉ cập nhật cụm Chatbox, giữ nguyên cấu hình kiểm duyệt hiện có.</summary>
    async Task<(bool Success, string? Error)> SaveAsync(bool enabled, string model, string systemPrompt,
        int? adminUserId, CancellationToken cancellationToken = default)
    {
        var current = await GetAsync(cancellationToken);
        var update = new AiSettingsUpdate(
            Enabled: enabled,
            SellerAssistantEnabled: current.SellerAssistantEnabled,
            Model: model,
            SystemPrompt: systemPrompt,
            ModerationEnabled: current.ModerationEnabled,
            ModerationModel: current.EffectiveModerationModel,
            ModerationMaxImages: current.ModerationMaxImages);
        return await SaveAsync(update, adminUserId, cancellationToken);
    }
}
