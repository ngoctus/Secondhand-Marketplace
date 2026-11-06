namespace SecondHandMarketplace.Web.Services.Ai;

public interface IAiChatService
{
    /// <summary>Chatbox đang bật hay tắt theo cấu hình Admin (đọc từ database ở mỗi lần gọi).</summary>
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Xử lý một tin nhắn của người dùng (mỗi lượt độc lập, không lưu lịch sử hội thoại).
    /// Không ném exception cho các lỗi nghiệp vụ/nhà cung cấp: trả về <see cref="AiChatResult"/> với thông báo an toàn.
    /// Chỉ ném OperationCanceledException khi chính người gọi hủy (ví dụ client ngắt kết nối).
    /// </summary>
    /// <param name="userId">Chỉ dùng để ghi log.</param>
    /// <param name="role">USER / ADMIN: quyết định AI được phép gọi công cụ nào.</param>
    Task<AiChatResult> AskAsync(string userId, string role, string message, CancellationToken cancellationToken = default);
}
