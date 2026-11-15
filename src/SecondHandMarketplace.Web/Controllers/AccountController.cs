using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Account;
using SecondHandMarketplace.Web.Services.Auth;

namespace SecondHandMarketplace.Web.Controllers;

public sealed class AccountController(
    IAuthService authService,
    IPasswordResetService passwordResetService,
    IRegistrationOtpService registrationOtpService,
    AppDbContext db) : Controller
{
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLogin(string provider, string? returnUrl = null, CancellationToken cancellationToken = default)
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

        var email = isGoogle ? "demo.google@remarket.vn" : "demo.facebook@remarket.vn";
        var fullName = isGoogle ? "Người dùng Google" : "Người dùng Facebook";
        var avatar = isGoogle
            ? "https://lh3.googleusercontent.com/a/default-user=s96-c"
            : "https://graph.facebook.com/v12.0/me/picture?type=large";

        var user = await db.NguoiDungs.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null)
        {
            user = new NguoiDung
            {
                HoTen = fullName,
                Email = email,
                SoDienThoai = isGoogle ? "0988888888" : "0977777777",
                MatKhauBam = "$2a$11$e1O8uK5c5wY6zN2T0I6A4.8V9p5D0Q1W2E3R4T5Y6U7I8O9P0A1B2",
                VaiTro = "USER",
                TrangThai = "ACTIVE",
                AnhDaiDien = avatar,
                NgayTao = DateTime.UtcNow,
                NgayCapNhat = DateTime.UtcNow
            };
            db.NguoiDungs.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }
        else if (user.TrangThai != "ACTIVE")
        {
            TempData["Loi"] = "Tài khoản của bạn đã bị khóa hoặc không hoạt động.";
            return RedirectToAction(nameof(Login));
        }

        await SignInUserAsync(user.MaNguoiDung, user.Email, user.HoTen, user.VaiTro, true, user.AnhDaiDien, DangNhapHelper.LaySecurityStamp(user.MatKhauBam));
        TempData["ThanhCong"] = $"Đăng nhập thành công với {prov}!";

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
        var user = await authService.AuthenticateAsync(model.Email, model.Password, cancellationToken);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác, hoặc tài khoản đã bị khóa.");
            return View(model);
        }
        await SignInUserAsync(user.MaNguoiDung, user.Email, user.HoTen, user.VaiTro, model.RememberMe, user.AnhDaiDien, DangNhapHelper.LaySecurityStamp(user.MatKhauBam));
        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return LocalRedirect(model.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.GetUserId(), out var userId)) return Challenge();
        var user = await authService.GetUserAsync(userId, cancellationToken);
        if (user is null) return NotFound();
        return View(new ProfileVm { HoTen = user.HoTen, Email = user.Email, SoDienThoai = user.SoDienThoai ?? string.Empty, DiaChiMacDinh = user.DiaChiMacDinh, AnhDaiDien = user.AnhDaiDien });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileVm model, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.GetUserId(), out var userId)) return Challenge();
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
        if (!int.TryParse(User.GetUserId(), out var userId)) return Challenge();
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

    private async Task SignInUserAsync(int id, string email, string fullName, string role, bool persistent, string? avatar = null, string? securityStamp = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id.ToString()), new(ClaimTypes.Name, fullName),
            new(ClaimTypes.Email, email), new(ClaimTypes.Role, role)
        };
        if (!string.IsNullOrWhiteSpace(avatar))
        {
            claims.Add(new("avatar", avatar));
        }
        if (!string.IsNullOrWhiteSpace(securityStamp))
        {
            claims.Add(new("sec_stamp", securityStamp));
        }
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = persistent, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });
    }
}
