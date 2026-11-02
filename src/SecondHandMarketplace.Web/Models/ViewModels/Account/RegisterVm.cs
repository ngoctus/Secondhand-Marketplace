using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.ViewModels.Account;

public class RegisterVm
{
    [Required(ErrorMessage = "Nhập họ tên")]
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    // Khớp ràng buộc CK_NguoiDung_Email trong DB (có @, có dấu chấm sau @, không khoảng trắng).
    [Required(ErrorMessage = "Nhập email")]
    [StringLength(256, ErrorMessage = "Email không được vượt quá 256 ký tự")]
    [RegularExpression(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9\-]+(\.[A-Za-z0-9\-]+)*\.[A-Za-z]{2,}$", ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    // DB bắt buộc USER phải có số điện thoại, chỉ gồm 9-15 chữ số (CK_NguoiDung_SDTBatBuoc, CK_NguoiDung_SoDienThoai).
    [Required(ErrorMessage = "Nhập số điện thoại")]
    [RegularExpression(@"^[0-9]{9,15}$", ErrorMessage = "Số điện thoại chỉ gồm 9 đến 15 chữ số")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập mật khẩu"), DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập lại mật khẩu")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [Display(Name = "Nhập lại mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;
}