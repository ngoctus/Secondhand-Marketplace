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
            await emailSender.SendAsync(email, "Mã xác nhận đặt lại mật khẩu Re:Market",
                $"<p>Bạn vừa yêu cầu đặt lại mật khẩu Re:Market.</p><p>Mã xác nhận của bạn là: <strong style='font-size:24px;letter-spacing:4px'>{safeCode}</strong></p><p>Mã có hiệu lực trong 10 phút. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>", cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            otp.UsedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            logger.LogError(ex, "Không gửi được email đặt lại mật khẩu tới {Email}.", email);
            return false;
        }
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken cancellationToken = default)
    {
        email = email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        var otp = await db.PasswordResetOtps.Where(x => x.Email == email && x.UsedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (otp is null || otp.ExpiresAtUtc <= now || otp.FailedAttempts >= 5)
            return (false, "Mã OTP đã hết hạn hoặc không còn hiệu lực. Hãy gửi mã mới.");

        var expected = Encoding.ASCII.GetBytes(_protector.Unprotect(otp.CodeProtected));
        var actual = Encoding.ASCII.GetBytes(code);
        if (expected.Length != actual.Length || !CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            otp.FailedAttempts++;
            if (otp.FailedAttempts >= 5) otp.UsedAtUtc = now;
            await db.SaveChangesAsync(cancellationToken);
            return (false, otp.FailedAttempts >= 5 ? "Bạn đã nhập sai quá số lần cho phép. Hãy gửi mã mới." : $"Mã OTP không chính xác. Còn {5 - otp.FailedAttempts} lần thử.");
        }

        var user = await db.NguoiDungs.SingleOrDefaultAsync(x => x.Email == email && x.TrangThai == "ACTIVE", cancellationToken);
        if (user is null) return (false, "Không thể đặt lại mật khẩu cho tài khoản này.");
        user.MatKhauBam = passwordService.Bam(newPassword);
        user.NgayCapNhat = now;
        otp.UsedAtUtc = now;
        foreach (var other in await db.PasswordResetOtps.Where(x => x.Email == email && x.Id != otp.Id && x.UsedAtUtc == null).ToListAsync(cancellationToken))
            other.UsedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }
}
