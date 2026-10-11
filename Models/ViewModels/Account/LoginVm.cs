using System.ComponentModel.DataAnnotations;

namespace Web2Hand.Models.ViewModels.Account;

public class LoginVm
{
    [Required(ErrorMessage = "Nhập email"), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập mật khẩu"), DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
