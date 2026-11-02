using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.ViewModels.Account;
using SecondHandMarketplace.Web.Services.Auth;

namespace SecondHandMarketplace.Web.Controllers;

public sealed class AccountController(IAuthService authService) : Controller
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
        var result = await authService.RegisterAsync(model.FullName, model.Email, model.Phone, model.Password, cancellationToken);
        if (!result.Success || result.User is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể tạo tài khoản.");
            return View(model);
        }
        await SignInUserAsync(result.User.MaNguoiDung, result.User.Email, result.User.HoTen, result.User.VaiTro, false);
        TempData["ThanhCong"] = "Đăng ký tài khoản thành công!";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new LoginVm { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
        await SignInUserAsync(user.MaNguoiDung, user.Email, user.HoTen, user.VaiTro, model.RememberMe);
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
            await SignInUserAsync(user.MaNguoiDung, user.Email, user.HoTen, user.VaiTro, persistent);
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

    private async Task SignInUserAsync(int id, string email, string fullName, string role, bool persistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id.ToString()), new(ClaimTypes.Name, fullName),
            new(ClaimTypes.Email, email), new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = persistent, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });
    }
}
