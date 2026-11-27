using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Services.Areas;

namespace SecondHandMarketplace.Web.Models.ViewModels.Shop;

public sealed class ShopVm
{
    public int? MaCuaHang { get; set; }
    [Required(ErrorMessage = "Shop_NameRequired"), StringLength(100, ErrorMessage = "Shop_FieldMaxLength")]
    [Display(Name = "Shop_Name")]
    public string TenShop { get; set; } = string.Empty;
    [Required(ErrorMessage = "Shop_PhoneRequired")]
    [RegularExpression(@"^0[35789][0-9]{8}$", ErrorMessage = "Shop_PhoneInvalid")]
    [Display(Name = "Shop_Phone")]
    public string SoDienThoaiLienHe { get; set; } = string.Empty;
    [StringLength(1000, ErrorMessage = "Shop_FieldMaxLength"), Display(Name = "Shop_Description")]
    public string? MoTa { get; set; }
    [StringLength(500, ErrorMessage = "Shop_FieldMaxLength"), Display(Name = "Shop_Logo")]
    public string? Logo { get; set; }
    public string TrangThai { get; set; } = "ACTIVE";

    // ---- Địa chỉ / khu vực (bắt buộc; shop cũ có cột NULL vẫn mở được form nhưng phải bổ sung khi lưu) ----
    [Required(ErrorMessage = "Shop_ProvinceRequired")]
    [Display(Name = "Shop_Province")]
    public int? MaTinhThanh { get; set; }

    [Required(ErrorMessage = "Shop_WardRequired")]
    [Display(Name = "Shop_Ward")]
    public int? MaPhuongXa { get; set; }

    [Required(ErrorMessage = "Shop_AddressRequired"), StringLength(300, ErrorMessage = "Shop_FieldMaxLength")]
    [Display(Name = "Shop_StreetAddress")]
    public string? DiaChiChiTiet { get; set; }

    [StringLength(100, ErrorMessage = "Shop_FieldMaxLength")]
    [Display(Name = "Shop_Neighborhood")]
    public string? KhuVucHienThi { get; set; }

    // ---- Thông tin nhận chuyển khoản (tùy chọn; đã nhập thì phải đủ ngân hàng, số tài khoản, tên chủ tài khoản) ----
    [StringLength(10)]
    [Display(Name = "Shop_Bank")]
    public string? MaNganHang { get; set; }

    /// <summary>Tên ngân hàng hiển thị, do server điền theo MaNganHang.</summary>
    public string? NganHang { get; set; }

    [StringLength(30, ErrorMessage = "Shop_FieldMaxLength")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "Shop_AccountDigitsOnly")]
    [Display(Name = "Shop_AccountNumber")]
    public string? SoTaiKhoan { get; set; }

    [StringLength(100, ErrorMessage = "Shop_FieldMaxLength")]
    [Display(Name = "Shop_AccountHolder")]
    public string? TenChuTaiKhoan { get; set; }

    // ---- Dữ liệu nạp cho dropdown, không nhận từ form ----
    [ValidateNever]
    public IReadOnlyList<TinhThanhItem> TinhThanhs { get; set; } = Array.Empty<TinhThanhItem>();

    [ValidateNever]
    public IReadOnlyList<PhuongXaItem> PhuongXas { get; set; } = Array.Empty<PhuongXaItem>();

    [ValidateNever]
    public IReadOnlyList<NganHangVietQr> NganHangs { get; set; } = VietQrBanks.All;
}
