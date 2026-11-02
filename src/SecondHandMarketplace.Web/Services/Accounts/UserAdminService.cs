using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Services.AuditLogs;

namespace SecondHandMarketplace.Web.Services.Accounts;

public sealed class UserAdminService(AppDbContext db, IAuditLogService auditLog) : IUserAdminService
{
    public async Task<(bool Success, string? Error)> LockUserAsync(int? actorId, int userId, string? reason, CancellationToken cancellationToken = default)
    {
        reason = reason?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length > 500)
            return (false, "Lý do khóa bắt buộc và tối đa 500 ký tự.");
        var user = await db.NguoiDungs.SingleOrDefaultAsync(x => x.MaNguoiDung == userId, cancellationToken);
        if (user is null) return (false, "Không tìm thấy tài khoản.");
        if (user.VaiTro == "ADMIN") return (false, "Không được khóa tài khoản ADMIN.");
        if (user.TrangThai == "LOCKED") return (true, null);

        user.TrangThai = "LOCKED";
        user.LyDoKhoa = reason;
        user.ThoiGianKhoa = DateTime.UtcNow;
        user.NgayCapNhat = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.LogAsync(actorId, "LOCK_USER", "NguoiDung", userId, "SUCCESS", reason);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UnlockUserAsync(int? actorId, int userId, CancellationToken cancellationToken = default)
    {
        var user = await db.NguoiDungs.SingleOrDefaultAsync(x => x.MaNguoiDung == userId, cancellationToken);
        if (user is null) return (false, "Không tìm thấy tài khoản.");
        if (user.TrangThai != "LOCKED") return (true, null);

        user.TrangThai = "ACTIVE";
        user.LyDoKhoa = null;
        user.ThoiGianKhoa = null;
        user.NgayCapNhat = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.LogAsync(actorId, "UNLOCK_USER", "NguoiDung", userId);
        return (true, null);
    }
}