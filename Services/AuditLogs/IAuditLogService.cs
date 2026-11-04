namespace SecondHandMarketplace.Web.Services.AuditLogs;

public interface IAuditLogService
{
    /// <summary>Gọi trong mọi hành động quan trọng của Admin (khóa user, duyệt/ẩn sản phẩm, xử lý tranh chấp...).</summary>
    Task LogAsync(string adminId, string action, string targetType, string targetId, string? detail = null);
}
