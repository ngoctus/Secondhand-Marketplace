using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.ViewModels.Shop;

public sealed class ShopVm
{
    public int? MaCuaHang { get; set; }
    [Required(ErrorMessage = "Nhập tên cửa hàng"), StringLength(100)]
    [Display(Name = "Tên cửa hàng")]
    public string TenShop { get; set; } = string.Empty;
    [Required(ErrorMessage = "Nhập số điện thoại liên hệ")]
    [RegularExpression("^[0-9]{9,15}$", ErrorMessage = "Số điện thoại gồm 9–15 chữ số")]
    [Display(Name = "Số điện thoại liên hệ")]
    public string SoDienThoaiLienHe { get; set; } = string.Empty;
    [StringLength(1000), Display(Name = "Mô tả")]
    public string? MoTa { get; set; }
    [StringLength(500), Display(Name = "Logo (đường dẫn ảnh)")]
    public string? Logo { get; set; }
    public string TrangThai { get; set; } = "ACTIVE";
}
