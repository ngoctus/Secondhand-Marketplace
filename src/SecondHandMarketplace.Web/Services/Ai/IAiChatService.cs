namespace SecondHandMarketplace.Web.Services.Ai;

public interface IAiChatService
{
    /// <summary>Chatbox đang bật hay tắt theo cấu hình Admin (đọc từ database ở mỗi lần gọi).</summary>
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>Trạng thái bật/tắt riêng của Chatbox và Trợ lý bán hàng (Viết mô tả + Tham khảo giá).</summary>
    async Task<AiFeatureStatus> GetFeatureStatusAsync(CancellationToken cancellationToken = default)
    {
        var enabled = await IsEnabledAsync(cancellationToken);
        return new AiFeatureStatus(enabled, enabled);
    }

    /// <summary>
    /// Xử lý một tin nhắn của người dùng (mỗi lượt độc lập, không lưu lịch sử hội thoại).
    /// Không ném exception cho các lỗi nghiệp vụ/nhà cung cấp: trả về <see cref="AiChatResult"/> với thông báo an toàn.
    /// Chỉ ném OperationCanceledException khi chính người gọi hủy (ví dụ client ngắt kết nối).
    /// </summary>
    /// <param name="userId">Chỉ dùng để ghi log.</param>
    /// <param name="role">USER / ADMIN: quyết định AI được phép gọi công cụ nào.</param>
    /// <param name="language">Ngôn ngữ mong muốn ("vi" | "en"); null thì tự đọc theo CultureInfo.CurrentUICulture.</param>
    Task<AiChatResult> AskAsync(string userId, string role, string message, string? language, CancellationToken cancellationToken = default);

    /// <summary>Quá tải tương thích ngược: tự lấy ngôn ngữ theo CultureInfo.CurrentUICulture.</summary>
    Task<AiChatResult> AskAsync(string userId, string role, string message, CancellationToken cancellationToken = default) =>
        AskAsync(userId, role, message, language: null, cancellationToken);

    /// <summary>
    /// Soạn mới hoặc trau chuốt mô tả sản phẩm cho người bán (POST /api/ai/draft-description).
    /// Dùng chung khóa API nhóm 1 (Ai:ApiKey + FallbackApiKey) và cấu hình Trợ lý bán hàng.
    /// </summary>
    Task<AiChatResult> DraftDescriptionAsync(string userId, string role, AiDescriptionDraftInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gợi ý khoảng giá bán dựa trên dữ liệu sản phẩm thật trên sàn kết hợp nhận xét AI (POST /api/ai/suggest-price).
    /// </summary>
    Task<AiPriceSuggestionResult> SuggestPriceAsync(string userId, string role, AiPriceSuggestInput input, CancellationToken cancellationToken = default);
}
