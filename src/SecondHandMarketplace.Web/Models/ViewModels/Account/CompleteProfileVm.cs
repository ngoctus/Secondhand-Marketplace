using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.ViewModels.Account;

public sealed class CompleteProfileVm
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [EmailAddress]
    [Display(Name = "Email liên kết")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^0[35789][0-9]{8}$", ErrorMessage = "Số điện thoại không hợp lệ (gồm 10 chữ số, bắt đầu bằng 03, 05, 07, 08, 09).")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Địa chỉ mặc định tối đa 500 ký tự.")]
    [Display(Name = "Địa chỉ mặc định (tùy chọn)")]
    public string? DiaChiMacDinh { get; set; }

    public string? ReturnUrl { get; set; }
}
