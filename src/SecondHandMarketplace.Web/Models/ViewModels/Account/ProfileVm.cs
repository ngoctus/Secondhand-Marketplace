using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.ViewModels.Account;

public sealed class ProfileVm
{
    [Required(ErrorMessage = "Nhập họ tên")]
    [StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập số điện thoại")]
    [RegularExpression("^[0-9]{9,15}$", ErrorMessage = "Số điện thoại gồm 9–15 chữ số")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Địa chỉ mặc định")]
    public string? DiaChiMacDinh { get; set; }

    [StringLength(500)]
    [Display(Name = "Đường dẫn ảnh đại diện")]
    public string? AnhDaiDien { get; set; }
}
