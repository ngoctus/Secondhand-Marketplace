using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.ViewModels.Account;

public sealed class ForgotPasswordVm
{
    [Required(ErrorMessage = "Nhập email đã đăng ký.")]
    [EmailAddress(ErrorMessage = "Email chưa đúng định dạng.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
}
