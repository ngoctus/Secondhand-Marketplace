using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Web2Hand.Common.Constants;
using Web2Hand.Models.Entities;
using Web2Hand.Models.ViewModels.Account;

namespace Web2Hand.Controllers;

/// <summary>Đăng ký / đăng nhập / đăng xuất cho cả 3 vai trò.</summary>
public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginVm { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _userManager.FindByEmailAsync(vm.Email);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError("", "Sai email hoặc mật khẩu, hoặc tài khoản đã bị khóa.");
            return View(vm);
        }

        var result = await _signInManager.PasswordSignInAsync(user, vm.Password, vm.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Sai email hoặc mật khẩu, hoặc tài khoản đã bị khóa.");
            return View(vm);
        }

        return LocalRedirect(Url.IsLocalUrl(vm.ReturnUrl) ? vm.ReturnUrl! : "/");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterVm());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = new AppUser
        {
            UserName = vm.Email,
            Email = vm.Email,
            FullName = vm.FullName,
            PhoneNumber = vm.Phone
        };

        var result = await _userManager.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return View(vm);
        }

        await _userManager.AddToRoleAsync(user, AppRoles.Buyer);   // ai đăng ký cũng là Buyer trước
        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();
}
