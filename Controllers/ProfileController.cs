using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Controllers;

/// <summary>Hồ sơ cá nhân: cập nhật thông tin, đổi mật khẩu, đăng ký trở thành Seller.</summary>
[Authorize]
public class ProfileController : Controller
{
    // TODO: Edit, ChangePassword, BecomeSeller (thêm role Seller bằng UserManager.AddToRoleAsync)
    public IActionResult Index() => View();
}
