using Web2Hand.Data;
using Web2Hand.Models.Entities;

namespace Web2Hand.Services.AuditLogs;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _db;

    public AuditLogService(AppDbContext db) => _db = db;

    public async Task LogAsync(string adminId, string action, string targetType, string targetId, string? detail = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            AdminId = adminId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Detail = detail
        });
        await _db.SaveChangesAsync();
    }
}
