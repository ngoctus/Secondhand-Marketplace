using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.ViewModels.Account;

public sealed class ResetPasswordOtpVm
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập mã OTP.")]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "Mã OTP gồm 6 chữ số.")]
    public string Code { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự."), DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận chưa khớp."), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
