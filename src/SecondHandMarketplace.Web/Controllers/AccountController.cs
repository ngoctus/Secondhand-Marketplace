using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.DataProtection;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Account;
using SecondHandMarketplace.Web.Services.Accounts;
using SecondHandMarketplace.Web.Services.Auth;

namespace SecondHandMarketplace.Web.Controllers;

public sealed class AccountController(
    IAuthService authService,
    IPasswordResetService passwordResetService,
    IRegistrationOtpService registrationOtpService,
    AppDbContext db,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    IAccountAppealUserService accountAppealUserService,
    IDataProtectionProvider dataProtectionProvider) : Controller
{
    private readonly IDataProtector _appealProtector = dataProtectionProvider.CreateProtector("AccountLockedAppeal");

    private string TaoTokenMoKhoa(int userId)
    {
        var payload = $"{userId}:{DateTime.UtcNow.Ticks}";
        return _appealProtector.Protect(payload);
    }

    private bool GiaiMaTokenMoKhoa(string? token, out int userId)
    {
        userId = 0;
        if (string.IsNullOrWhiteSpace(token)) return false;
        try
        {
            var raw = _appealProtector.Unprotect(token);
            var parts = raw.Split(':');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out userId)) return false;
            if (!long.TryParse(parts[1], out var ticks)) return false;

            var issuedAt = new DateTime(ticks, DateTimeKind.Utc);
            if (DateTime.UtcNow - issuedAt > TimeSpan.FromMinutes(10))
            {
                return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
    private bool IsMockOAuthEnabled() =>
        environment.IsDevelopment() && configuration.GetValue("MockOAuth:Enabled", true);

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View("AccessDenied");

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new RegisterVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm model, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        if (!ModelState.IsValid) return View(model);

        try
        {
            var (success, error, token) = await registrationOtpService.StartRegistrationAsync(model, cancellationToken);
            if (!success || string.IsNullOrWhiteSpace(token))
            {
                ModelState.AddModelError(string.Empty, error ?? "Không thể tiến hành đăng ký. Vui lòng thử lại.");
                return View(model);
            }

            TempData["ThongBao"] = $"Mã xác nhận OTP 6 chữ số đã được gửi tới email {model.Email.Trim()}. Vui lòng nhập mã để hoàn tất đăng ký.";
            return RedirectToAction(nameof(VerifyRegistration), new { email = model.Email.Trim().ToLowerInvariant(), token });
        }
        catch (OperationCanceledException)
        {
            ModelState.AddModelError(string.Empty, "Yêu cầu đăng ký bị quá thời gian chờ xử lý. Vui lòng thử lại.");
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult VerifyRegistration(string email, string token)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction(nameof(Register));
        return View(new RegisterVerifyVm { Email = email ?? string.Empty, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyRegistration(RegisterVerifyVm model, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        if (!ModelState.IsValid) return View(model);

        try
        {
            var (success, error, user, newToken) = await registrationOtpService.VerifyOtpAsync(model.Token, model.Code, cancellationToken);
            if (!success || user is null)
            {
                if (!string.IsNullOrWhiteSpace(newToken))
                {
                    model.Token = newToken;
                }
                ModelState.AddModelError(string.Empty, error ?? "Mã OTP không hợp lệ.");
                return View(model);
            }

            await SignInUserAsync(user.MaNguoiDung, user.Email, user.HoTen, user.VaiTro, false, user.AnhDaiDien, DangNhapHelper.LaySecurityStamp(user.MatKhauBam));
            TempData["ThanhCong"] = "Đăng ký tài khoản và xác thực email thành công! Chào mừng bạn đến với Re:Market.";
            return RedirectToAction("Index", "Home");
        }
        catch (OperationCanceledException)
        {
            ModelState.AddModelError(string.Empty, "Yêu cầu xác thực OTP bị quá thời gian chờ xử lý. Vui lòng thử lại.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendRegisterCode(RegisterVerifyVm model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Token)) return RedirectToAction(nameof(Register));
        try
        {
            var (success, error, newToken) = await registrationOtpService.ResendCodeAsync(model.Token, cancellationToken);
            if (success)
            {
                TempData["ThongBao"] = "Đã gửi lại mã xác nhận OTP tới email của bạn. Vui lòng kiểm tra hộp thư.";
            }
            else
            {
                TempData["Loi"] = error ?? "Chưa thể gửi lại mã xác nhận. Vui lòng thử lại sau.";
            }
            return RedirectToAction(nameof(VerifyRegistration), new { email = model.Email?.Trim().ToLowerInvariant(), token = newToken ?? model.Token });
        }
        catch (OperationCanceledException)
        {
            TempData["Loi"] = "Yêu cầu gửi lại mã bị quá thời gian chờ. Vui lòng thử lại.";
            return RedirectToAction(nameof(VerifyRegistration), new { email = model.Email?.Trim().ToLowerInvariant(), token = model.Token });
        }
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new LoginVm { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new ForgotPasswordVm());
    }

    [AllowAnonymous]
    [HttpPost, ValidateAntiForgeryToken]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordVm model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            var sent = await passwordResetService.SendCodeAsync(model.Email, cancellationToken);
            if (!sent)
            {
                ModelState.AddModelError(string.Empty, "Chưa gửi được email xác nhận. Vui lòng thử lại sau.");
                return View(model);
            }
            TempData["ThongBao"] = "Mã xác nhận OTP 6 chữ số đã được gửi tới email " + model.Email.Trim() + ". Vui lòng kiểm tra hộp thư (kể cả mục Spam).";
            return RedirectToAction(nameof(ResetPassword), new { email = model.Email.Trim().ToLowerInvariant() });
        }
        catch (OperationCanceledException)
        {
            ModelState.AddModelError(string.Empty, "Yêu cầu gửi mã xác nhận bị quá thời gian chờ. Vui lòng thử lại.");
            return View(model);
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResetPassword(string email) => View(new ResetPasswordOtpVm { Email = email ?? string.Empty });

    [AllowAnonymous]
    [HttpPost, ValidateAntiForgeryToken]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ResetPassword(ResetPasswordOtpVm model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            var result = await passwordResetService.ResetPasswordAsync(model.Email, model.Code, model.NewPassword, cancellationToken);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Không thể đặt lại mật khẩu.");
                return View(model);
            }
            TempData["ThanhCong"] = "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới.";
            return RedirectToAction(nameof(Login));
        }
        catch (OperationCanceledException)
        {
            ModelState.AddModelError(string.Empty, "Yêu cầu đặt lại mật khẩu bị quá thời gian chờ. Vui lòng thử lại.");
            return View(model);
        }
    }

    [AllowAnonymous]
    [HttpPost, ValidateAntiForgeryToken]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ResendResetCode(ForgotPasswordVm model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(ForgotPassword));
        try
        {
            var sent = await passwordResetService.SendCodeAsync(model.Email, cancellationToken);
            if (sent)
            {
                TempData["ThongBao"] = "Đã gửi lại mã xác nhận OTP tới email " + model.Email.Trim() + ". Vui lòng kiểm tra hộp thư.";
            }
            else
            {
                TempData["Loi"] = "Chưa gửi được email xác nhận. Vui lòng thử lại sau.";
            }
        }
        catch (OperationCanceledException)
        {
            TempData["Loi"] = "Yêu cầu gửi lại mã bị quá thời gian chờ. Vui lòng thử lại.";
        }
        return RedirectToAction(nameof(ResetPassword), new { email = model.Email.Trim().ToLowerInvariant() });
    }

    [HttpPost, HttpGet]
    [AllowAnonymous]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");

        var prov = (provider ?? "Google").Trim();
        var isGoogle = prov.Equals("Google", StringComparison.OrdinalIgnoreCase);
        var isFacebook = prov.Equals("Facebook", StringComparison.OrdinalIgnoreCase);

        if (!isGoogle && !isFacebook)
        {
            TempData["Loi"] = "Nhà cung cấp đăng nhập không được hỗ trợ.";
            return RedirectToAction(nameof(Login));
        }

        var googleClientId = configuration["Authentication:Google:ClientId"];
        var isGoogleConfigured = isGoogle && !string.IsNullOrWhiteSpace(googleClientId) &&
                                 !googleClientId.StartsWith("CHANGE_THIS", StringComparison.OrdinalIgnoreCase);

        var fbAppId = configuration["Authentication:Facebook:AppId"];
        var isFbConfigured = isFacebook && !string.IsNullOrWhiteSpace(fbAppId) &&
                             !fbAppId.StartsWith("CHANGE_THIS", StringComparison.OrdinalIgnoreCase);

        if (isGoogleConfigured || isFbConfigured)
        {
            var scheme = isGoogle
                ? Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme
                : Microsoft.AspNetCore.Authentication.Facebook.FacebookDefaults.AuthenticationScheme;
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, scheme);
        }

        // Khi chưa cấu hình khóa API thực tế, mở màn hình chọn tài khoản trên máy người dùng (Google/Facebook Account Chooser)
        if (!IsMockOAuthEnabled() && !environment.IsDevelopment())
        {
            TempData["Loi"] = $"Đăng nhập bằng {prov} chưa được cấu hình.";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }
        return RedirectToAction(nameof(ExternalLoginSelectAccount), new { provider = prov, returnUrl });
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var authResult = await HttpContext.AuthenticateAsync("ExternalCookie");
        if (!authResult.Succeeded || authResult.Principal == null)
        {
            TempData["Loi"] = "Đăng nhập bên ngoài không thành công hoặc bạn đã hủy xác thực.";
            return RedirectToAction(nameof(Login));
        }

        var email = authResult.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            await HttpContext.SignOutAsync("ExternalCookie");
            TempData["Loi"] = "Không thể lấy địa chỉ email từ tài khoản liên kết.";
            return RedirectToAction(nameof(Login));
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await db.NguoiDungs.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (user is not null && user.TrangThai != "ACTIVE")
        {
            await HttpContext.SignOutAsync("ExternalCookie");
            if (string.Equals(user.TrangThai, "LOCKED", StringComparison.OrdinalIgnoreCase))
            {
                var token = TaoTokenMoKhoa(user.MaNguoiDung);
                return RedirectToAction(nameof(Locked), new { token });
            }
            TempData["Loi"] = "Tài khoản của bạn đã bị khóa hoặc không hoạt động.";
            return RedirectToAction(nameof(Login));
        }

        // Nếu đăng nhập mạng xã hội lần đầu hoặc tài khoản chưa có số điện thoại hợp lệ -> chuyển sang Hoàn tất hồ sơ
        if (user is null || string.IsNullOrWhiteSpace(user.SoDienThoai) || user.SoDienThoai == "0988888888")
        {
            return RedirectToAction(nameof(CompleteProfile), new { returnUrl });
        }
        await HttpContext.SignOutAsync("ExternalCookie");
        await SignInUserAsync(user.MaNguoiDung, user.Email, user.HoTen, user.VaiTro, true, user.AnhDaiDien, DangNhapHelper.LaySecurityStamp(user.MatKhauBam));
        TempData["ThanhCong"] = "Đăng nhập thành công!";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteProfile(string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");

        var authResult = await HttpContext.AuthenticateAsync("ExternalCookie");
        if (!authResult.Succeeded || authResult.Principal == null)
        {
            TempData["Loi"] = "Phiên xác thực mạng xã hội đã hết hạn. Vui lòng đăng nhập lại.";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var email = authResult.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            await HttpContext.SignOutAsync("ExternalCookie");
            TempData["Loi"] = "Không tìm thấy email từ tài khoản liên kết.";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var fullName = authResult.Principal.FindFirstValue(ClaimTypes.Name) ?? normalizedEmail.Split('@')[0];
        var existingUser = await db.NguoiDungs.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        return View(new CompleteProfileVm
        {
            Email = normalizedEmail,
            HoTen = existingUser?.HoTen ?? fullName.Trim(),
            DiaChiMacDinh = existingUser?.DiaChiMacDinh,
            ReturnUrl = returnUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteProfile(CompleteProfileVm model, CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");

        var authResult = await HttpContext.AuthenticateAsync("ExternalCookie");
        if (!authResult.Succeeded || authResult.Principal == null)
        {
            TempData["Loi"] = "Phiên xác thực mạng xã hội đã hết hạn. Vui lòng đăng nhập lại.";
            return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
        }

        var email = authResult.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            await HttpContext.SignOutAsync("ExternalCookie");
            TempData["Loi"] = "Không tìm thấy email từ tài khoản liên kết.";
            return RedirectToAction(nameof(Login));
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        model.Email = normalizedEmail;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedPhone = model.SoDienThoai.Trim();
        var phoneInUse = await db.NguoiDungs.AnyAsync(
            u => u.Email != normalizedEmail && u.SoDienThoai == normalizedPhone,
            cancellationToken);
        if (phoneInUse)
        {
            ModelState.AddModelError(nameof(model.SoDienThoai), "Số điện thoại này đã được sử dụng bởi tài khoản khác.");
            return View(model);
        }

        var avatar = authResult.Principal.FindFirstValue("picture")
                     ?? authResult.Principal.FindFirstValue("urn:google:picture");

        var user = await db.NguoiDungs.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (user is null)
        {
            user = new NguoiDung
            {
                HoTen = model.HoTen.Trim(),
                Email = normalizedEmail,
                SoDienThoai = normalizedPhone,
                DiaChiMacDinh = string.IsNullOrWhiteSpace(model.DiaChiMacDinh) ? null : model.DiaChiMacDinh.Trim(),
                MatKhauBam = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                VaiTro = "USER",
                TrangThai = "ACTIVE",
                AnhDaiDien = avatar,
                NgayTao = DateTime.UtcNow,
                NgayCapNhat = DateTime.UtcNow
            };
            db.NguoiDungs.Add(user);
        }
        else
        {
            if (user.TrangThai != "ACTIVE")
            {
                await HttpContext.SignOutAsync("ExternalCookie");
                if (string.Equals(user.TrangThai, "LOCKED", StringComparison.OrdinalIgnoreCase))
                {
                    var token = TaoTokenMoKhoa(user.MaNguoiDung);
                    return RedirectToAction(nameof(Locked), new { token });
                }
                TempData["Loi"] = "Tài khoản của bạn đã bị khóa hoặc không hoạt động.";
                return RedirectToAction(nameof(Login));
            }
            user.HoTen = model.HoTen.Trim();
            user.SoDienThoai = normalizedPhone;
            if (!string.IsNullOrWhiteSpace(model.DiaChiMacDinh))
            {
                user.DiaChiMacDinh = model.DiaChiMacDinh.Trim();
            }
            user.NgayCapNhat = DateTime.UtcNow;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.SoDienThoai), "Số điện thoại đã được sử dụng hoặc thông tin không hợp lệ.");
            return View(model);
        }

        await HttpContext.SignOutAsync("ExternalCookie");
        await SignInUserAsync(user.MaNguoiDung, user.Email, user.HoTen, user.VaiTro, true, user.AnhDaiDien, DangNhapHelper.LaySecurityStamp(user.MatKhauBam));
        TempData["ThanhCong"] = "Hoàn tất hồ sơ và đăng nhập thành công!";

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginSelectAccount(string provider = "Google", string? returnUrl = null)
    {
        if (!IsMockOAuthEnabled() && !environment.IsDevelopment()) return NotFound();
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");

        var prov = (provider ?? "Google").Trim();
        var isGoogle = prov.Equals("Google", StringComparison.OrdinalIgnoreCase);

        var accounts = await db.NguoiDungs
            .Where(u => u.TrangThai == "ACTIVE" && u.VaiTro == "USER")
            .OrderBy(u => u.MaNguoiDung)
            .Take(4)
            .Select(u => new ExternalAccountItemVm
            {
                HoTen = u.HoTen,
                Email = u.Email,
                AnhDaiDien = u.AnhDaiDien
            })
            .ToListAsync();

        ViewBag.Provider = isGoogle ? "Google" : "Facebook";
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.Accounts = accounts;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginSelectAccountSubmit(
        string provider,
        string email,
        string? fullName,
        string? returnUrl,
        CancellationToken cancellationToken = default)
    {
        if (!IsMockOAuthEnabled() && !environment.IsDevelopment()) return NotFound();
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");

        var prov = (provider ?? "Google").Trim();
        var isGoogle = prov.Equals("Google", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            TempData["Loi"] = "Vui lòng nhập địa chỉ email hợp lệ để tiếp tục.";
            return RedirectToAction(nameof(ExternalLoginSelectAccount), new { provider = prov, returnUrl });
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var name = string.IsNullOrWhiteSpace(fullName)
            ? normalizedEmail.Split('@')[0]
            : fullName.Trim();

        var avatar = isGoogle
            ? "https://lh3.googleusercontent.com/a/default-user=s96-c"
            : "https://graph.facebook.com/v12.0/me/picture?type=large";

        var user = await db.NguoiDungs.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (user is not null && user.TrangThai != "ACTIVE")
        {
            if (string.Equals(user.TrangThai, "LOCKED", StringComparison.OrdinalIgnoreCase))
            {
                var token = TaoTokenMoKhoa(user.MaNguoiDung);
                return RedirectToAction(nameof(Locked), new { token });
            }
            TempData["Loi"] = "Tài khoản của bạn đã bị khóa hoặc không hoạt động.";
            return RedirectToAction(nameof(Login));
        }

        if (user is null || string.IsNullOrWhiteSpace(user.SoDienThoai) || user.SoDienThoai == "0988888888")
        {
            var extClaims = new List<Claim>
            {
                new(ClaimTypes.Email, normalizedEmail),
                new(ClaimTypes.Name, user?.HoTen ?? name)
            };
            var pic = user?.AnhDaiDien ?? avatar;
            if (!string.IsNullOrWhiteSpace(pic))
            {
                extClaims.Add(new("picture", pic));
            }
            var extIdentity = new ClaimsIdentity(extClaims, "ExternalCookie");
            await HttpContext.SignInAsync("ExternalCookie", new ClaimsPrincipal(extIdentity));
            return RedirectToAction(nameof(CompleteProfile), new { returnUrl });
        }

        await SignInUserAsync(user.MaNguoiDung, user.Email, user.HoTen, user.VaiTro, true, user.AnhDaiDien, DangNhapHelper.LaySecurityStamp(user.MatKhauBam));
        TempData["ThanhCong"] = $"Đăng nhập thành công với {prov} ({user.Email})!";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginVm model, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        if (!ModelState.IsValid) return View(model);

        var result = await authService.AuthenticateWithStatusAsync(model.Email, model.Password, cancellationToken);
        switch (result.Status)
        {
            case AuthResultStatus.Success:
                var user = result.User!;
                await SignInUserAsync(user.MaNguoiDung, user.Email, user.HoTen, user.VaiTro, model.RememberMe, user.AnhDaiDien, DangNhapHelper.LaySecurityStamp(user.MatKhauBam));
                if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return LocalRedirect(model.ReturnUrl);
                return RedirectToAction("Index", "Home");

            case AuthResultStatus.AccountLocked:
                var lockedToken = TaoTokenMoKhoa(result.User!.MaNguoiDung);
                return RedirectToAction(nameof(Locked), new { token = lockedToken });

            case AuthResultStatus.TempLockout:
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Bạn đã nhập sai mật khẩu quá nhiều lần. Vui lòng thử lại sau ít phút.");
                return View(model);

            case AuthResultStatus.InvalidCredentials:
            default:
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
                return View(model);
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Locked(string? token, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra nếu có token hợp lệ
        if (GiaiMaTokenMoKhoa(token, out var userId))
        {
            var overview = await accountAppealUserService.GetLockedAccountOverviewAsync(userId, cancellationToken);
            if (overview == null || !overview.DangBiKhoa)
            {
                TempData["ThongBao"] = "Tài khoản của bạn hiện đang hoạt động bình thường.";
                return RedirectToAction(nameof(Login));
            }

            return View(new LockedAccountVm
            {
                Token = token,
                RequiresTokenVerification = false,
                Overview = overview,
                LoaiYeuCau = overview.ChoPhepKhieuNaiKhoaNham ? AccountAppealRules.TypeAppeal : AccountAppealRules.TypeUnlockRequest
            });
        }

        // 2. Không có token hoặc token không hợp lệ / hết hạn
        return View(new LockedAccountVm
        {
            Token = null,
            RequiresTokenVerification = true,
            Overview = null
        });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("appeal")]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> SubmitAppeal(SubmitAppealFormVm form, CancellationToken cancellationToken)
    {
        if (!GiaiMaTokenMoKhoa(form.Token, out var userId))
        {
            TempData["Loi"] = "Phiên làm việc khiếu nại đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại để tiếp tục.";
            return RedirectToAction(nameof(Locked));
        }

        if (!ModelState.IsValid)
        {
            var overview = await accountAppealUserService.GetLockedAccountOverviewAsync(userId, cancellationToken);
            return View(nameof(Locked), new LockedAccountVm
            {
                Token = form.Token,
                RequiresTokenVerification = false,
                Overview = overview,
                LoaiYeuCau = form.LoaiYeuCau,
                NoiDung = form.NoiDung
            });
        }

        string? bangChungAnh = null;
        string? savedFullPath = null;

        if (form.FileDinhKem != null && form.FileDinhKem.Length > 0)
        {
            var (hopLe, loi, ext) = await UploadAnhAnToan.KiemTraAnhHopLeAsync(form.FileDinhKem, cancellationToken: cancellationToken);
            if (!hopLe)
            {
                TempData["Loi"] = loi;
                return RedirectToAction(nameof(Locked), new { token = form.Token });
            }

            var webRoot = environment?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var (relPath, fullPath) = await UploadAnhAnToan.LuuAnhAsync(form.FileDinhKem, webRoot, "appeals", ext!, cancellationToken);
            bangChungAnh = relPath;
            savedFullPath = fullPath;
        }

        try
        {
            var result = await accountAppealUserService.SubmitAppealAsync(
                userId,
                form.LoaiYeuCau,
                form.NoiDung,
                bangChungAnh,
                cancellationToken);

            if (!result.Success)
            {
                UploadAnhAnToan.XoaAnh(savedFullPath);
                TempData["Loi"] = result.Error ?? "Không thể gửi yêu cầu khiếu nại.";
                return RedirectToAction(nameof(Locked), new { token = form.Token });
            }

            TempData["ThanhCong"] = "Đã gửi yêu cầu thành công! Quản trị viên sẽ xem xét và phản hồi trong thời gian sớm nhất.";
            return RedirectToAction(nameof(Locked), new { token = form.Token });
        }
        catch
        {
            UploadAnhAnToan.XoaAnh(savedFullPath);
            throw;
        }
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        if (User.LayNguoiDungId() is not int userId) return Challenge();
        var user = await authService.GetUserAsync(userId, cancellationToken);
        if (user is null) return NotFound();
        return View(new ProfileVm { HoTen = user.HoTen, Email = user.Email, SoDienThoai = user.SoDienThoai ?? string.Empty, DiaChiMacDinh = user.DiaChiMacDinh, AnhDaiDien = user.AnhDaiDien });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileVm model, CancellationToken cancellationToken)
    {
        if (User.LayNguoiDungId() is not int userId) return Challenge();
        if (!ModelState.IsValid) return View(model);
        var result = await authService.UpdateProfileAsync(userId, model.HoTen, model.SoDienThoai, model.DiaChiMacDinh, model.AnhDaiDien, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật hồ sơ.");
            return View(model);
        }
        var user = await authService.GetUserAsync(userId, cancellationToken);
        if (user is not null)
        {
            var persistent = (await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme)).Properties?.IsPersistent ?? false;
            await SignInUserAsync(user.MaNguoiDung, user.Email, user.HoTen, user.VaiTro, persistent, user.AnhDaiDien, DangNhapHelper.LaySecurityStamp(user.MatKhauBam));
        }
        TempData["ThanhCong"] = "Đã cập nhật hồ sơ.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordVm());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordVm model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        if (User.LayNguoiDungId() is not int userId) return Challenge();
        var result = await authService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể đổi mật khẩu.");
            return View(model);
        }
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["ThanhCong"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    private Task SignInUserAsync(int id, string email, string fullName, string role, bool persistent, string? avatar = null, string? securityStamp = null) =>
        HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            DangNhapHelper.TaoPrincipal(id, email, fullName, role, avatar, securityStamp),
            new AuthenticationProperties { IsPersistent = persistent, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });
}
