using Microsoft.EntityFrameworkCore;
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
    public async Task<NguoiDung?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await db.NguoiDungs.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (user is null)
        {
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
            await GhiNhatKyAsync(user.MaNguoiDung, "LOGIN", user.MaNguoiDung, "FAILED", "Mật khẩu không chính xác.");
            return null;
        }

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
