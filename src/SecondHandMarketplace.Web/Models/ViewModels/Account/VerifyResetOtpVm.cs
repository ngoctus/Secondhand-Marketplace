using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.ViewModels.Account;

public sealed class VerifyResetOtpVm
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập mã OTP.")]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "Mã OTP gồm 6 chữ số.")]
    public string Code { get; set; } = string.Empty;
}
