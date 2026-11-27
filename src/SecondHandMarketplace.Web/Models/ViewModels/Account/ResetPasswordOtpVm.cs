using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.ViewModels.Account;

public sealed class ResetPasswordOtpVm
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    public string? VerifiedToken { get; set; }

    [RegularExpression("^[0-9]{6}$", ErrorMessage = "Mã OTP gồm 6 chữ số.")]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới"), DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
        ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự, bao gồm ít nhất 1 chữ hoa, 1 chữ thường, 1 chữ số và 1 ký tự đặc biệt.")]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận chưa khớp."), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
