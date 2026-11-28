using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Security;

namespace SecondHandMarketplace.Web.Services.Auth;

public sealed class PasswordResetService(
    AppDbContext db,
    IEmailSender emailSender,
    IDataProtectionProvider protectionProvider,
    IMatKhauService passwordService,
    ILogger<PasswordResetService> logger) : IPasswordResetService
{
    private readonly IDataProtector _protector = protectionProvider.CreateProtector("ReMarket.PasswordResetOtp.v1");
    private readonly IDataProtector _verifiedProtector = protectionProvider.CreateProtector("ReMarket.PasswordResetVerified.v1");

    public async Task<bool> SendCodeAsync(string email, CancellationToken cancellationToken = default)
    {
        email = email.Trim().ToLowerInvariant();
        var userExists = await db.NguoiDungs.AnyAsync(x => x.Email == email && x.TrangThai == "ACTIVE", cancellationToken);
        if (!userExists) return true; // Không tiết lộ email có đăng ký hay không.

        var now = DateTime.UtcNow;
        var latest = await db.PasswordResetOtps.Where(x => x.Email == email)
            .OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (latest is not null && latest.CreatedAtUtc > now.AddMinutes(-1)) return true;

        foreach (var old in await db.PasswordResetOtps.Where(x => x.Email == email && x.UsedAtUtc == null).ToListAsync(cancellationToken))
            old.UsedAtUtc = now;

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var otp = new PasswordResetOtp
        {
            Email = email,
            CodeProtected = _protector.Protect(code),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(10)
        };
        db.PasswordResetOtps.Add(otp);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var safeCode = System.Net.WebUtility.HtmlEncode(code);
            logger.LogInformation("[OTP-RESET] Đã gửi mã xác nhận đặt lại mật khẩu cho {Email}", email);
            await emailSender.SendAsync(email, "Mã xác nhận đổi / đặt lại mật khẩu Re:Market",
                $"<p>Bạn vừa yêu cầu xác thực OTP để đổi hoặc đặt lại mật khẩu Re:Market.</p><p>Mã xác nhận của bạn là: <strong style='font-size:24px;letter-spacing:4px'>{safeCode}</strong></p><p>Mã có hiệu lực trong 10 phút. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>", cancellationToken);
            return true;
        }
        catch (OperationCanceledException oce)
        {
            otp.UsedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
            logger.LogWarning(oce, "Thao tác gửi email đặt lại mật khẩu tới {Email} bị hủy hoặc quá thời gian chờ.", email);
            return false;
        }
        catch (Exception ex)
        {
            otp.UsedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
            logger.LogError(ex, "Không gửi được email đặt lại mật khẩu tới {Email}.", email);
            return false;
        }
    }

    public async Task<(bool Success, string? Error, string? VerifiedToken)> VerifyCodeAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        email = (email ?? string.Empty).Trim().ToLowerInvariant();
        code = (code ?? string.Empty).Trim();
        var now = DateTime.UtcNow;

        var otp = await db.PasswordResetOtps.Where(x => x.Email == email && x.UsedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (otp is null || otp.ExpiresAtUtc <= now || otp.FailedAttempts >= 5)
            return (false, "Mã OTP đã hết hạn hoặc không còn hiệu lực. Hãy gửi mã mới.", null);

        var expected = Encoding.ASCII.GetBytes(_protector.Unprotect(otp.CodeProtected));
        var actual = Encoding.ASCII.GetBytes(code);
        if (expected.Length != actual.Length || !CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            otp.FailedAttempts++;
            if (otp.FailedAttempts >= 5) otp.UsedAtUtc = now;
            await db.SaveChangesAsync(cancellationToken);
            var msg = otp.FailedAttempts >= 5
                ? "Bạn đã nhập sai quá số lần cho phép. Hãy gửi mã mới."
                : $"Mã OTP không chính xác. Còn {5 - otp.FailedAttempts} lần thử.";
            return (false, msg, null);
        }

        var expiresAtTicks = now.AddMinutes(10).Ticks;
        var verifiedToken = _verifiedProtector.Protect($"{email}|{otp.Id}|{expiresAtTicks}");
        return (true, null, verifiedToken);
    }

    public async Task<(bool Success, string? Error)> ConsumeVerifiedTokenAsync(string email, string verifiedToken, CancellationToken cancellationToken = default)
    {
        var (ok, error, otp) = await ValidateVerifiedTokenAsync(email, verifiedToken, cancellationToken);
        if (!ok || otp is null) return (false, error);

        var now = DateTime.UtcNow;
        otp.UsedAtUtc = now;
        foreach (var other in await db.PasswordResetOtps.Where(x => x.Email == otp.Email && x.Id != otp.Id && x.UsedAtUtc == null).ToListAsync(cancellationToken))
            other.UsedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordWithTokenAsync(
        int userId, string currentPassword, string newPassword, string verifiedToken, CancellationToken cancellationToken = default)
    {
        var user = await db.NguoiDungs.SingleOrDefaultAsync(x => x.MaNguoiDung == userId, cancellationToken);
        if (user is null) return (false, "Không tìm thấy tài khoản.");
        if (!passwordService.KiemTra(currentPassword, user.MatKhauBam))
            return (false, "Mật khẩu hiện tại không chính xác.");

        var (ok, error, otp) = await ValidateVerifiedTokenAsync(user.Email, verifiedToken, cancellationToken);
        if (!ok || otp is null) return (false, error);

        var now = DateTime.UtcNow;
        user.MatKhauBam = passwordService.Bam(newPassword);
        user.NgayCapNhat = now;
        otp.UsedAtUtc = now;
        foreach (var other in await db.PasswordResetOtps
                     .Where(x => x.Email == otp.Email && x.Id != otp.Id && x.UsedAtUtc == null)
                     .ToListAsync(cancellationToken))
        {
            other.UsedAtUtc = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordWithTokenAsync(string email, string verifiedToken, string newPassword, CancellationToken cancellationToken = default)
    {
        var (ok, error, otp) = await ValidateVerifiedTokenAsync(email, verifiedToken, cancellationToken);
        if (!ok || otp is null) return (false, error);

        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        var user = await db.NguoiDungs.SingleOrDefaultAsync(x => x.Email == normalizedEmail && x.TrangThai == "ACTIVE", cancellationToken);
        if (user is null) return (false, "Không thể đặt lại mật khẩu cho tài khoản này.");

        var now = DateTime.UtcNow;
        user.MatKhauBam = passwordService.Bam(newPassword);
        user.NgayCapNhat = now;
        otp.UsedAtUtc = now;
        foreach (var other in await db.PasswordResetOtps.Where(x => x.Email == normalizedEmail && x.Id != otp.Id && x.UsedAtUtc == null).ToListAsync(cancellationToken))
            other.UsedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    private async Task<(bool Success, string? Error, PasswordResetOtp? Otp)> ValidateVerifiedTokenAsync(string email, string verifiedToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(verifiedToken))
            return (false, "Phiên xác thực OTP không hợp lệ. Vui lòng nhập lại mã OTP.", null);

        string raw;
        try
        {
            raw = _verifiedProtector.Unprotect(verifiedToken);
        }
        catch
        {
            return (false, "Phiên xác thực OTP không hợp lệ hoặc đã hết hạn. Vui lòng xác thực lại OTP.", null);
        }

        var parts = raw.Split('|');
        if (parts.Length != 3
            || !int.TryParse(parts[1], out var otpId)
            || !long.TryParse(parts[2], out var expiresTicks))
        {
            return (false, "Phiên xác thực OTP không hợp lệ.", null);
        }

        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (!string.Equals(parts[0], normalizedEmail, StringComparison.OrdinalIgnoreCase))
            return (false, "Phiên xác thực OTP không khớp với email tài khoản.", null);

        var now = DateTime.UtcNow;
        if (new DateTime(expiresTicks, DateTimeKind.Utc) <= now)
            return (false, "Phiên xác thực OTP đã hết hạn. Vui lòng gửi mã OTP mới.", null);

        var otp = await db.PasswordResetOtps.SingleOrDefaultAsync(x => x.Id == otpId && x.Email == normalizedEmail && x.UsedAtUtc == null, cancellationToken);
        if (otp is null || otp.ExpiresAtUtc <= now || otp.FailedAttempts >= 5)
            return (false, "Mã OTP đã được sử dụng hoặc đã hết hạn. Vui lòng gửi mã mới.", null);

        return (true, null, otp);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken cancellationToken = default)
    {
        var (ok, error, verifiedToken) = await VerifyCodeAsync(email, code, cancellationToken);
        if (!ok || string.IsNullOrEmpty(verifiedToken))
            return (false, error);

        return await ResetPasswordWithTokenAsync(email, verifiedToken, newPassword, cancellationToken);
    }
}
