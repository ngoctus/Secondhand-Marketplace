using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Security;

namespace SecondHandMarketplace.Web.Services.Auth;

public sealed class AuthService(AppDbContext db, IMatKhauService passwordService) : IAuthService
{
    public async Task<NguoiDung?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await db.NguoiDungs.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (user is null || !string.Equals(user.TrangThai, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            return null;
        return passwordService.KiemTra(password, user.MatKhauBam) ? user : null;
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
            return (true, null, user);
        }
        catch (DbUpdateException)
        {
            db.Entry(user).State = EntityState.Detached;
            return (false, "Email hoặc số điện thoại đã được đăng ký.", null);
        }
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
        return (true, null);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
