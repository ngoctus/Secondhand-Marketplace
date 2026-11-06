namespace SecondHandMarketplace.Web.Services.Ai;

/// <summary>Đọc/ghi cấu hình Chatbox AI (bật/tắt, model, System Prompt) trong database.</summary>
public interface IAiSettingsService
{
    /// <summary>Danh sách model Admin được phép chọn.</summary>
    IReadOnlyList<string> AllowedModels { get; }

    /// <summary>
    /// Đọc cấu hình hiện hành TỪ DATABASE ở mỗi lần gọi (không cache) để thay đổi của Admin có hiệu lực ngay.
    /// Khi bảng/dòng cấu hình chưa tồn tại, trả về mặc định (IsPersisted = false) thay vì ném lỗi.
    /// </summary>
    Task<AiSettings> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Kiểm tra và lưu cấu hình. Trả (false, lỗi) khi dữ liệu không hợp lệ hoặc database chưa có bảng cấu hình.</summary>
    Task<(bool Success, string? Error)> SaveAsync(bool enabled, string model, string systemPrompt,
        int? adminUserId, CancellationToken cancellationToken = default);
}
