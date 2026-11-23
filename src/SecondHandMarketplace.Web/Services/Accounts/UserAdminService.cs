using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SecondHandMarketplace.Web.Controllers;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Services.AuditLogs;

namespace SecondHandMarketplace.Web.Services.Accounts;

public sealed class UserAdminService(AppDbContext db, IAuditLogService auditLog, IMemoryCache? cache = null) : IUserAdminService
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

        var now = DateTime.UtcNow;
        user.TrangThai = "LOCKED";
        user.LyDoKhoa = reason;
        user.ThoiGianKhoa = now;
        user.SoLanBiKhoa = Math.Max(0, user.SoLanBiKhoa) + 1;
        user.NgayCapNhat = now;

        var shops = await db.CuaHangs.Where(c => c.MaNguoiDung == userId && c.TrangThai == "ACTIVE").ToListAsync(cancellationToken);
        foreach (var s in shops)
        {
            s.TrangThai = "LOCKED";
            s.LyDoKhoa = reason;
            s.ThoiGianKhoa = now;
            s.NgayCapNhat = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        cache?.Remove(HomeController.CatalogCacheKey);
        await auditLog.LogAsync(actorId, "LOCK_USER", "NguoiDung", userId, "SUCCESS", reason);
        return (true, null);
    }

    public Task<(bool Success, string? Error)> UnlockUserAsync(int? actorId, int userId, CancellationToken cancellationToken = default) =>
        UnlockUserAsync(actorId, userId, giamSoLanBiKhoa: false, cancellationToken);

    public async Task<(bool Success, string? Error)> UnlockUserAsync(int? actorId, int userId, bool giamSoLanBiKhoa, CancellationToken cancellationToken = default)
    {
        var user = await db.NguoiDungs.SingleOrDefaultAsync(x => x.MaNguoiDung == userId, cancellationToken);
        if (user is null) return (false, "Không tìm thấy tài khoản.");
        if (user.TrangThai != "LOCKED") return (true, null);

        var now = DateTime.UtcNow;
        user.TrangThai = "ACTIVE";
        user.LyDoKhoa = null;
        user.ThoiGianKhoa = null;
        user.MocTinhViPham = now;
        if (giamSoLanBiKhoa)
            user.SoLanBiKhoa = Math.Max(0, user.SoLanBiKhoa - 1);
        user.NgayCapNhat = now;

        var shops = await db.CuaHangs.Where(c => c.MaNguoiDung == userId && c.TrangThai == "LOCKED").ToListAsync(cancellationToken);
        foreach (var s in shops)
        {
            s.TrangThai = "ACTIVE";
            s.LyDoKhoa = null;
            s.ThoiGianKhoa = null;
            s.NgayCapNhat = now;
        }

        // Nếu Admin mở khóa trực tiếp tại Admin/Users mà user đang có đơn khiếu nại PENDING, tự động đóng đơn để tránh treo.
        var pendingAppeals = await db.KhieuNaiTaiKhoans
            .Where(k => k.MaNguoiDung == userId && k.TrangThai == AccountAppealRules.StatusPending)
            .ToListAsync(cancellationToken);
        foreach (var appeal in pendingAppeals)
        {
            if (appeal.TrangThai != AccountAppealRules.StatusPending) continue;
            appeal.TrangThai = AccountAppealRules.StatusApproved;
            appeal.GhiChuAdmin = "Tài khoản đã được mở khóa trực tiếp bởi quản trị viên.";
            appeal.NguoiXuLyId = actorId;
            appeal.NgayXuLy = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        cache?.Remove(HomeController.CatalogCacheKey);
        await auditLog.LogAsync(actorId, "UNLOCK_USER", "NguoiDung", userId);
        return (true, null);
    }
}
