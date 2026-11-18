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
    private readonly IDataProtector _protector = protectionProvider.CreateProtector("ReMarket.RegistrationOtp.v2");

    /// <summary>
    /// Payload gọn gửi về client dưới dạng token đã mã hóa (KHÔNG chứa mật khẩu gốc, mã OTP hay bộ đếm thử sai).
    /// </summary>
    public sealed class PendingRegistrationPayload
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }

    /// <summary>
    /// Trạng thái đăng ký chờ kích hoạt chỉ lưu trên máy chủ (IMemoryCache gắn theo Email).
    /// </summary>
    private sealed class PendingRegistrationServerState
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string OtpHash { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public int FailedAttempts { get; set; }
    }

    private static string GetStateCacheKey(string normalizedEmail) => $"reg_otp_state_{normalizedEmail}";
    private static string GetCooldownCacheKey(string normalizedEmail) => $"reg_otp_cooldown_{normalizedEmail}";

    private static string HashOtp(string normalizedEmail, string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{normalizedEmail}:{code.Trim()}"));
        return Convert.ToHexString(bytes);
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

        var cooldownKey = GetCooldownCacheKey(normalizedEmail);
        var stateKey = GetStateCacheKey(normalizedEmail);
        if (cache.TryGetValue(cooldownKey, out string? cachedToken) &&
            !string.IsNullOrWhiteSpace(cachedToken) &&
            cache.TryGetValue(stateKey, out PendingRegistrationServerState? _))
        {
            logger.LogInformation("[OTP-REGISTER] Đang có mã OTP còn hiệu lực cho {Email}, sử dụng lại token hiện tại tránh gửi trùng.", normalizedEmail);
            return (true, null, cachedToken);
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var now = DateTime.UtcNow;
        var fullName = (model.FullName ?? string.Empty).Trim();

        try
        {
            var safeCode = System.Net.WebUtility.HtmlEncode(code);
            logger.LogInformation("[OTP-REGISTER] Mã xác nhận đăng ký tài khoản cho {Email} là: {Code}", normalizedEmail, code);
            await emailSender.SendAsync(normalizedEmail, "Mã xác nhận đăng ký tài khoản Re:Market",
                $"<div style='font-family:sans-serif;max-width:560px;margin:auto;padding:24px;border:1px solid #e2e8f0;border-radius:12px;background:#ffffff'>" +
                $"<h2 style='color:#68705A;margin-top:0'>Xác nhận đăng ký tài khoản Re:Market</h2>" +
                $"<p>Chào <strong>{System.Net.WebUtility.HtmlEncode(fullName)}</strong>,</p>" +
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

        var expiresAt = now.AddMinutes(10);
        var serverState = new PendingRegistrationServerState
        {
            FullName = fullName,
            Email = normalizedEmail,
            Phone = normalizedPhone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            OtpHash = HashOtp(normalizedEmail, code),
            CreatedAtUtc = now,
            ExpiresAtUtc = expiresAt,
            FailedAttempts = 0
        };
        cache.Set(stateKey, serverState, TimeSpan.FromMinutes(10));

        var payload = new PendingRegistrationPayload
        {
            FullName = fullName,
            Email = normalizedEmail,
            Phone = normalizedPhone,
            CreatedAtUtc = now,
            ExpiresAtUtc = expiresAt
        };

        var token = _protector.Protect(JsonSerializer.Serialize(payload));
        cache.Set(cooldownKey, token, TimeSpan.FromSeconds(60));
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

        if (payload is null || string.IsNullOrWhiteSpace(payload.Email))
        {
            return (false, "Dữ liệu đăng ký không hợp lệ. Vui lòng đăng ký lại.", null, null);
        }

        var normalizedEmail = payload.Email.Trim().ToLowerInvariant();
        var stateKey = GetStateCacheKey(normalizedEmail);
        if (!cache.TryGetValue(stateKey, out PendingRegistrationServerState? state) || state is null)
        {
            return (false, "Phiên xác thực đăng ký đã hết hạn hoặc không tồn tại. Vui lòng đăng ký lại.", null, null);
        }

        var now = DateTime.UtcNow;
        if (payload.ExpiresAtUtc <= now || state.ExpiresAtUtc <= now)
        {
            cache.Remove(stateKey);
            return (false, "Mã OTP đã hết hiệu lực (quá 10 phút). Vui lòng nhấn 'Gửi lại mã OTP' hoặc đăng ký lại.", null, null);
        }

        if (state.FailedAttempts >= 5)
        {
            return (false, "Bạn đã nhập sai OTP quá 5 lần. Vui lòng thực hiện đăng ký lại từ đầu.", null, null);
        }

        var expected = Encoding.ASCII.GetBytes(state.OtpHash);
        var actual = Encoding.ASCII.GetBytes(HashOtp(normalizedEmail, code ?? string.Empty));

        if (expected.Length != actual.Length || !CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            state.FailedAttempts++;
            var ttl = state.ExpiresAtUtc > now ? state.ExpiresAtUtc - now : TimeSpan.FromMinutes(1);
            cache.Set(stateKey, state, ttl);

            var remaining = Math.Max(0, 5 - state.FailedAttempts);
            var msg = state.FailedAttempts >= 5
                ? "Bạn đã nhập sai mã OTP quá 5 lần. Vui lòng gửi lại mã mới hoặc đăng ký lại."
                : $"Mã OTP không chính xác. Bạn còn {remaining} lần thử.";
            return (false, msg, null, token);
        }

        // OTP chính xác, tiến hành lưu tài khoản vào cơ sở dữ liệu với mật khẩu đã băm trên máy chủ
        var regResult = await authService.RegisterAsync(
            state.FullName, state.Email, state.Phone, state.PasswordHash, cancellationToken);

        if (!regResult.Success || regResult.User is null)
        {
            return (false, regResult.Error ?? "Không thể hoàn tất đăng ký tài khoản.", null, null);
        }

        cache.Remove(stateKey);
        cache.Remove(GetCooldownCacheKey(normalizedEmail));
        logger.LogInformation("Người dùng {Email} đã xác thực OTP thành công và tạo tài khoản #{UserId}", state.Email, regResult.User.MaNguoiDung);
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

        if (payload is null || string.IsNullOrWhiteSpace(payload.Email))
        {
            return (false, "Dữ liệu đăng ký không hợp lệ. Vui lòng đăng ký lại.", null);
        }

        var normalizedEmail = payload.Email.Trim().ToLowerInvariant();
        var stateKey = GetStateCacheKey(normalizedEmail);
        if (!cache.TryGetValue(stateKey, out PendingRegistrationServerState? state) || state is null)
        {
            return (false, "Phiên đăng ký đã hết hạn. Vui lòng thực hiện đăng ký lại.", null);
        }

        var now = DateTime.UtcNow;
        if (state.CreatedAtUtc > now.AddSeconds(-60))
        {
            var secondsWait = (int)(60 - (now - state.CreatedAtUtc).TotalSeconds);
            return (false, $"Vui lòng chờ {Math.Max(1, secondsWait)} giây trước khi yêu cầu gửi lại mã OTP.", token);
        }

        var newCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        try
        {
            var safeCode = System.Net.WebUtility.HtmlEncode(newCode);
            logger.LogInformation("[OTP-REGISTER] Mã xác nhận đăng ký mới cho {Email} là: {Code}", state.Email, newCode);
            await emailSender.SendAsync(state.Email, "Mã xác nhận đăng ký tài khoản Re:Market (Gửi lại)",
                $"<div style='font-family:sans-serif;max-width:560px;margin:auto;padding:24px;border:1px solid #e2e8f0;border-radius:12px;background:#ffffff'>" +
                $"<h2 style='color:#68705A;margin-top:0'>Mã xác nhận mới cho tài khoản Re:Market</h2>" +
                $"<p>Chào <strong>{System.Net.WebUtility.HtmlEncode(state.FullName)}</strong>,</p>" +
                $"<p>Bạn vừa yêu cầu gửi lại mã xác nhận OTP đăng ký tài khoản. Mã mới của bạn là:</p>" +
                $"<div style='text-align:center;margin:24px 0'><span style='display:inline-block;padding:12px 28px;background:#f4f5f0;color:#68705A;font-size:30px;font-weight:700;letter-spacing:6px;border-radius:8px;border:2px dashed #68705A'>{safeCode}</span></div>" +
                $"<p style='color:#64748b;font-size:14px'>Mã OTP này có hiệu lực trong vòng <strong>10 phút</strong>.</p>" +
                $"</div>", cancellationToken);
        }
        catch (OperationCanceledException oce)
        {
            logger.LogWarning(oce, "Thao tác gửi lại email OTP tới {Email} bị quá thời gian chờ hoặc kết nối bị gián đoạn.", state.Email);
            return (false, "Yêu cầu gửi lại mã xác nhận bị quá thời gian chờ. Vui lòng thử lại sau.", token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Không thể gửi lại email OTP tới {Email}.", state.Email);
            return (false, "Chưa gửi được email xác nhận mới. Vui lòng thử lại sau.", token);
        }

        var expiresAt = now.AddMinutes(10);
        state.OtpHash = HashOtp(normalizedEmail, newCode);
        state.CreatedAtUtc = now;
        state.ExpiresAtUtc = expiresAt;
        state.FailedAttempts = 0;
        cache.Set(stateKey, state, TimeSpan.FromMinutes(10));

        payload.CreatedAtUtc = now;
        payload.ExpiresAtUtc = expiresAt;
        var newToken = _protector.Protect(JsonSerializer.Serialize(payload));
        cache.Set(GetCooldownCacheKey(normalizedEmail), newToken, TimeSpan.FromSeconds(60));
        return (true, null, newToken);
    }
}
