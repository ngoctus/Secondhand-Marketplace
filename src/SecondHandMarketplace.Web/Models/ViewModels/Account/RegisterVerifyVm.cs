using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.ViewModels.Account;

public sealed class RegisterVerifyVm
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mã OTP.")]
    [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Mã OTP gồm 6 chữ số.")]
    [Display(Name = "Mã xác nhận OTP")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phiên xác thực không hợp lệ. Vui lòng đăng ký lại.")]
    public string Token { get; set; } = string.Empty;
}
