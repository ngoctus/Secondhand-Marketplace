using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Account;

namespace SecondHandMarketplace.Web.Services.Auth;

public sealed class RegistrationOtpService(
    AppDbContext db,
    IEmailSender emailSender,
    IDataProtectionProvider protectionProvider,
    IAuthService authService,
    IMemoryCache cache,
    ILogger<RegistrationOtpService> logger) : IRegistrationOtpService
{
    private readonly IDataProtector _protector = protectionProvider.CreateProtector("ReMarket.RegistrationOtp.v1");

    public sealed class PendingRegistrationPayload
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public int FailedAttempts { get; set; }
    }

    public async Task<(bool Success, string? Error, string? Token)> StartRegistrationAsync(RegisterVm model, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedPhone = (model.Phone ?? string.Empty).Trim();

        if (await db.NguoiDungs.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
        {
            return (false, "Email này đã được sử dụng. Vui lòng đăng nhập hoặc sử dụng email khác.", null);
        }

        if (await db.NguoiDungs.AnyAsync(x => x.SoDienThoai == normalizedPhone, cancellationToken))
        {
            return (false, "Số điện thoại này đã được sử dụng. Vui lòng kiểm tra lại.", null);
        }

        var cacheKey = $"reg_otp_cooldown_{normalizedEmail}";
        if (cache.TryGetValue(cacheKey, out string? cachedToken) && !string.IsNullOrWhiteSpace(cachedToken))
        {
            logger.LogInformation("[OTP-REGISTER] Đang có mã OTP còn hiệu lực cho {Email}, sử dụng lại token hiện tại tránh gửi trùng.", normalizedEmail);
            return (true, null, cachedToken);
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var now = DateTime.UtcNow;

        var payload = new PendingRegistrationPayload
        {
            FullName = (model.FullName ?? string.Empty).Trim(),
            Email = normalizedEmail,
            Phone = normalizedPhone,
            Password = model.Password,
            Code = code,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(10),
            FailedAttempts = 0
        };

        var json = JsonSerializer.Serialize(payload);
        var token = _protector.Protect(json);

        try
        {
            var safeCode = System.Net.WebUtility.HtmlEncode(code);
            logger.LogInformation("[OTP-REGISTER] Đã gửi mã xác nhận OTP đăng ký tài khoản cho {Email}", normalizedEmail);
            await emailSender.SendAsync(normalizedEmail, "Mã xác nhận đăng ký tài khoản Re:Market",
                $"<div style='font-family:sans-serif;max-width:560px;margin:auto;padding:24px;border:1px solid #e2e8f0;border-radius:12px;background:#ffffff'>" +
                $"<h2 style='color:#68705A;margin-top:0'>Xác nhận đăng ký tài khoản Re:Market</h2>" +
                $"<p>Chào <strong>{System.Net.WebUtility.HtmlEncode(payload.FullName)}</strong>,</p>" +
                $"<p>Cảm ơn bạn đã đăng ký tham gia sàn giao dịch đồ cũ Re:Market. Mã xác nhận OTP của bạn là:</p>" +
                $"<div style='text-align:center;margin:24px 0'><span style='display:inline-block;padding:12px 28px;background:#f4f5f0;color:#68705A;font-size:30px;font-weight:700;letter-spacing:6px;border-radius:8px;border:2px dashed #68705A'>{safeCode}</span></div>" +
                $"<p style='color:#64748b;font-size:14px'>Mã OTP này có hiệu lực trong vòng <strong>10 phút</strong>. Vì lý do bảo mật, vui lòng không chia sẻ mã này cho bất kỳ ai.</p>" +
                $"<hr style='border:none;border-top:1px solid #e2e8f0;margin:20px 0' />" +
                $"<p style='color:#94a3b8;font-size:12px;margin-bottom:0'>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.</p>" +
                $"</div>", cancellationToken);
        }
        catch (OperationCanceledException oce)
        {
            logger.LogWarning(oce, "Thao tác gửi OTP đăng ký tới {Email} bị quá thời gian chờ hoặc kết nối bị gián đoạn.", normalizedEmail);
            return (false, "Yêu cầu gửi mã xác nhận bị quá thời gian chờ hoặc kết nối bị gián đoạn. Vui lòng thử lại.", null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Không thể gửi email OTP đăng ký tới {Email}.", normalizedEmail);
            return (false, "Chưa gửi được email xác nhận tới hộp thư của bạn. Vui lòng kiểm tra lại địa chỉ email hoặc thử lại sau.", null);
        }

        // Do not report an active OTP challenge unless the email was accepted for sending.
        cache.Set(cacheKey, token, TimeSpan.FromSeconds(60));
        cache.Remove($"reg_otp_attempts_{normalizedEmail}");
        return (true, null, token);
    }

    public async Task<(bool Success, string? Error, NguoiDung? User, string? NewToken)> VerifyOtpAsync(string token, string code, CancellationToken cancellationToken = default)
    {
        PendingRegistrationPayload? payload;
        try
        {
            var json = _protector.Unprotect(token);
            payload = JsonSerializer.Deserialize<PendingRegistrationPayload>(json);
        }
        catch
        {
            return (false, "Phiên xác thực đăng ký không hợp lệ hoặc đã hết hạn. Vui lòng đăng ký lại.", null, null);
        }

        if (payload is null)
        {
            return (false, "Dữ liệu đăng ký không hợp lệ. Vui lòng đăng ký lại.", null, null);
        }

        var now = DateTime.UtcNow;
        if (payload.ExpiresAtUtc <= now)
        {
            return (false, "Mã OTP đã hết hiệu lực (quá 10 phút). Vui lòng thực hiện đăng ký lại từ đầu.", null, null);
        }

        var attemptsKey = $"reg_otp_attempts_{payload.Email}";
        var failedAttempts = cache.Get<int?>(attemptsKey) ?? payload.FailedAttempts;
        if (failedAttempts >= 5)
        {
            return (false, "Bạn đã nhập sai OTP quá 5 lần. Vui lòng thực hiện đăng ký lại từ đầu.", null, null);
        }

        var expected = Encoding.ASCII.GetBytes(payload.Code);
        var actual = Encoding.ASCII.GetBytes((code ?? string.Empty).Trim());

        if (expected.Length != actual.Length || !CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            failedAttempts++;
            cache.Set(attemptsKey, failedAttempts, TimeSpan.FromMinutes(15));
            payload.FailedAttempts = failedAttempts;
            var updatedToken = _protector.Protect(JsonSerializer.Serialize(payload));
            var remaining = Math.Max(0, 5 - failedAttempts);
            var msg = failedAttempts >= 5
                ? "Bạn đã nhập sai mã OTP quá 5 lần. Vui lòng thực hiện đăng ký lại từ đầu."
                : $"Mã OTP không chính xác. Bạn còn {remaining} lần thử.";
            return (false, msg, null, updatedToken);
        }

        // OTP chính xác, tiến hành lưu tài khoản vào cơ sở dữ liệu
        var regResult = await authService.RegisterAsync(
            payload.FullName, payload.Email, payload.Phone, payload.Password, cancellationToken);

        if (!regResult.Success || regResult.User is null)
        {
            return (false, regResult.Error ?? "Không thể hoàn tất đăng ký tài khoản.", null, null);
        }

        cache.Remove(attemptsKey);
        cache.Remove($"reg_otp_cooldown_{payload.Email}");
        cache.Remove($"reg_otp_resend_{payload.Email}");
        logger.LogInformation("Người dùng {Email} đã xác thực OTP thành công và tạo tài khoản #{UserId}", payload.Email, regResult.User.MaNguoiDung);
        return (true, null, regResult.User, null);
    }

    public async Task<(bool Success, string? Error, string? NewToken)> ResendCodeAsync(string token, CancellationToken cancellationToken = default)
    {
        PendingRegistrationPayload? payload;
        try
        {
            var json = _protector.Unprotect(token);
            payload = JsonSerializer.Deserialize<PendingRegistrationPayload>(json);
        }
        catch
        {
            return (false, "Phiên đăng ký không hợp lệ hoặc đã hết hạn. Vui lòng đăng ký lại.", null);
        }

        if (payload is null)
        {
            return (false, "Dữ liệu đăng ký không hợp lệ. Vui lòng đăng ký lại.", null);
        }

        var now = DateTime.UtcNow;
        if (payload.ExpiresAtUtc <= now)
        {
            return (false, "Phiên đăng ký đã hết hạn. Vui lòng thực hiện đăng ký lại từ đầu.", null);
        }

        var attemptsKey = $"reg_otp_attempts_{payload.Email}";
        var failedAttempts = cache.Get<int?>(attemptsKey) ?? payload.FailedAttempts;
        if (failedAttempts >= 5)
        {
            return (false, "Bạn đã nhập sai OTP quá 5 lần. Vui lòng thực hiện đăng ký lại từ đầu.", null);
        }

        var resendCooldownKey = $"reg_otp_resend_{payload.Email}";
        if (cache.TryGetValue(resendCooldownKey, out _))
        {
            return (false, "Vui lòng đợi 60 giây trước khi yêu cầu gửi lại mã OTP.", token);
        }

        var newCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        payload.Code = newCode;
        payload.CreatedAtUtc = now;
        payload.ExpiresAtUtc = now.AddMinutes(10);
        payload.FailedAttempts = failedAttempts;

        try
        {
            var safeCode = System.Net.WebUtility.HtmlEncode(newCode);
            logger.LogInformation("[OTP-REGISTER] Đã gửi lại mã xác nhận đăng ký mới cho {Email}", payload.Email);
            await emailSender.SendAsync(payload.Email, "Mã xác nhận đăng ký tài khoản Re:Market (Gửi lại)",
                $"<div style='font-family:sans-serif;max-width:560px;margin:auto;padding:24px;border:1px solid #e2e8f0;border-radius:12px;background:#ffffff'>" +
                $"<h2 style='color:#68705A;margin-top:0'>Mã xác nhận mới cho tài khoản Re:Market</h2>" +
                $"<p>Chào <strong>{System.Net.WebUtility.HtmlEncode(payload.FullName)}</strong>,</p>" +
                $"<p>Bạn vừa yêu cầu gửi lại mã xác nhận OTP đăng ký tài khoản. Mã mới của bạn là:</p>" +
                $"<div style='text-align:center;margin:24px 0'><span style='display:inline-block;padding:12px 28px;background:#f4f5f0;color:#68705A;font-size:30px;font-weight:700;letter-spacing:6px;border-radius:8px;border:2px dashed #68705A'>{safeCode}</span></div>" +
                $"<p style='color:#64748b;font-size:14px'>Mã OTP này có hiệu lực trong vòng <strong>10 phút</strong>.</p>" +
                $"</div>", cancellationToken);
        }
        catch (OperationCanceledException oce)
        {
            logger.LogWarning(oce, "Thao tác gửi lại email OTP tới {Email} bị quá thời gian chờ hoặc kết nối bị gián đoạn.", payload.Email);
            return (false, "Yêu cầu gửi lại mã xác nhận bị quá thời gian chờ. Vui lòng thử lại sau.", token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Không thể gửi lại email OTP tới {Email}.", payload.Email);
            return (false, "Chưa gửi được email xác nhận mới. Vui lòng thử lại sau.", token);
        }

        cache.Set(resendCooldownKey, true, TimeSpan.FromSeconds(60));
        var newToken = _protector.Protect(JsonSerializer.Serialize(payload));
        cache.Set($"reg_otp_cooldown_{payload.Email}", newToken, TimeSpan.FromSeconds(60));
        return (true, null, newToken);
    }
}
