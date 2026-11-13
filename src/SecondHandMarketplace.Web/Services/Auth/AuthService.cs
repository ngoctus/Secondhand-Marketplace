using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Security;
using SecondHandMarketplace.Web.Services.AuditLogs;

namespace SecondHandMarketplace.Web.Services.Auth;

public sealed class AuthService(
    AppDbContext db,
    IMatKhauService passwordService,
    IAuditLogService audit,
    ILogger<AuthService> logger) : IAuthService
{
    private const string DummyHash = "$2a$11$0000000000000000000000uL1y6E0j5bVvL9tWj4K6M2EaN.aL0u6";
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int FailedCount, DateTime LockoutEnd)> AccountLockouts = new(StringComparer.OrdinalIgnoreCase);
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<NguoiDung?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);

        if (AccountLockouts.TryGetValue(normalizedEmail, out var lockout) && lockout.LockoutEnd > DateTime.UtcNow)
        {
            await GhiNhatKyAsync(null, "LOGIN", null, "LOCKED", $"Tài khoản bị khóa tạm đến {lockout.LockoutEnd:HH:mm:ss} UTC do sai mật khẩu {MaxFailedAttempts} lần liên tiếp.");
            return null;
        }

        var user = await db.NguoiDungs.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (user is null)
        {
            // Chống timing attack lộ sự tồn tại của email
            passwordService.KiemTra(password, DummyHash);
            await GhiNhatKyAsync(null, "LOGIN", null, "FAILED", "Email không tồn tại.");
            return null;
        }
        if (!string.Equals(user.TrangThai, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            await GhiNhatKyAsync(user.MaNguoiDung, "LOGIN", user.MaNguoiDung, "DENIED", "Tài khoản đang bị khóa.");
            return null;
        }
        if (!passwordService.KiemTra(password, user.MatKhauBam))
        {
            var attempts = AccountLockouts.AddOrUpdate(
                normalizedEmail,
                _ => (1, DateTime.MinValue),
                (_, current) => (current.FailedCount + 1, current.FailedCount + 1 >= MaxFailedAttempts ? DateTime.UtcNow.Add(LockoutDuration) : DateTime.MinValue)
            );

            if (attempts.FailedCount >= MaxFailedAttempts)
            {
                logger.LogWarning("Tài khoản {Email} bị khóa tạm thời 15 phút do nhập sai mật khẩu 5 lần liên tiếp.", normalizedEmail);
                await GhiNhatKyAsync(user.MaNguoiDung, "LOCKOUT", user.MaNguoiDung, "LOCKED", "Khóa tạm 15 phút do nhập sai mật khẩu 5 lần liên tiếp.");
            }
            else
            {
                await GhiNhatKyAsync(user.MaNguoiDung, "LOGIN", user.MaNguoiDung, "FAILED", $"Mật khẩu không chính xác ({attempts.FailedCount}/{MaxFailedAttempts}).");
            }
            return null;
        }

        AccountLockouts.TryRemove(normalizedEmail, out _);
        await GhiNhatKyAsync(user.MaNguoiDung, "LOGIN", user.MaNguoiDung, "SUCCESS");
        return user;
    }

    public async Task<(bool Success, string? Error, NguoiDung? User)> RegisterAsync(
        string fullName, string email, string phone, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var normalizedPhone = phone.Trim();
        if (await db.NguoiDungs.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
            return (false, "Email đã được đăng ký.", null);
        if (await db.NguoiDungs.AnyAsync(x => x.SoDienThoai == normalizedPhone, cancellationToken))
            return (false, "Số điện thoại đã được đăng ký.", null);

        var user = new NguoiDung
        {
            HoTen = fullName.Trim(), Email = normalizedEmail, SoDienThoai = normalizedPhone,
            MatKhauBam = passwordService.Bam(password), VaiTro = "USER", TrangThai = "ACTIVE",
            NgayTao = DateTime.UtcNow, NgayCapNhat = DateTime.UtcNow
        };
        db.NguoiDungs.Add(user);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.Entry(user).State = EntityState.Detached;
            return (false, "Email hoặc số điện thoại đã được đăng ký.", null);
        }

        await GhiNhatKyAsync(user.MaNguoiDung, "REGISTER", user.MaNguoiDung, "SUCCESS", chiTiet: $"Email: {user.Email}");
        return (true, null, user);
    }

    public Task<NguoiDung?> GetUserAsync(int userId, CancellationToken cancellationToken = default) =>
        db.NguoiDungs.AsNoTracking().SingleOrDefaultAsync(x => x.MaNguoiDung == userId, cancellationToken);

    public async Task<(bool Success, string? Error)> UpdateProfileAsync(
        int userId, string fullName, string? phone, string? address, string? avatar, CancellationToken cancellationToken = default)
    {
        var user = await db.NguoiDungs.SingleOrDefaultAsync(x => x.MaNguoiDung == userId, cancellationToken);
        if (user is null) return (false, "Không tìm thấy tài khoản.");
        if (!string.IsNullOrWhiteSpace(avatar) && !UrlAnToan.HopLe(avatar))
            return (false, "Ảnh đại diện phải là URL http/https.");
        var normalizedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        if (normalizedPhone is not null && await db.NguoiDungs.AnyAsync(x => x.MaNguoiDung != userId && x.SoDienThoai == normalizedPhone, cancellationToken))
            return (false, "Số điện thoại đã được sử dụng.");

        user.HoTen = fullName.Trim();
        user.SoDienThoai = normalizedPhone;
        user.DiaChiMacDinh = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        if (!string.IsNullOrWhiteSpace(avatar)) user.AnhDaiDien = avatar.Trim();
        user.NgayCapNhat = DateTime.UtcNow;
        try { await db.SaveChangesAsync(cancellationToken); return (true, null); }
        catch (DbUpdateException) { return (false, "Thông tin cập nhật không hợp lệ hoặc số điện thoại đã được sử dụng."); }
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(
        int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await db.NguoiDungs.SingleOrDefaultAsync(x => x.MaNguoiDung == userId, cancellationToken);
        if (user is null) return (false, "Không tìm thấy tài khoản.");
        if (!passwordService.KiemTra(currentPassword, user.MatKhauBam)) return (false, "Mật khẩu hiện tại không chính xác.");
        user.MatKhauBam = passwordService.Bam(newPassword);
        user.NgayCapNhat = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await GhiNhatKyAsync(userId, "CHANGE_PASSWORD", userId, "SUCCESS");
        return (true, null);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>
    /// Ghi nhật ký sau khi thao tác đã lưu. Lỗi nhật ký (hoặc In-Memory không có sp_GhiNhatKy)
    /// không được làm hỏng luồng đăng ký/đăng nhập/đổi mật khẩu.
    /// </summary>
    private async Task GhiNhatKyAsync(int? maNguoiDung, string hanhDong, int? doiTuongId, string ketQua,
        string? lyDo = null, string? chiTiet = null)
    {
        if (!db.Database.IsRelational()) return;
        try
        {
            await audit.LogAsync(maNguoiDung, hanhDong, "NguoiDung", doiTuongId, ketQua, lyDo, chiTiet);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không ghi được nhật ký {HanhDong}.", hanhDong);
        }
    }
}
