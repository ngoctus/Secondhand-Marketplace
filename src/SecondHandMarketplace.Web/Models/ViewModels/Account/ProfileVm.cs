using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.Areas;

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

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^0[35789][0-9]{8}$", ErrorMessage = "Số điện thoại không hợp lệ (gồm 10 chữ số, bắt đầu bằng 03, 05, 07, 08, 09)")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Địa chỉ mặc định")]
    public string? DiaChiMacDinh { get; set; }

    public int? MaTinhThanh { get; set; }
    public int? MaPhuongXa { get; set; }

    [StringLength(300)]
    public string? DiaChiChiTiet { get; set; }

    [StringLength(500)]
    [Display(Name = "Đường dẫn ảnh đại diện")]
    public string? AnhDaiDien { get; set; }

    public IFormFile? AvatarFile { get; set; }

    [ValidateNever]
    public IReadOnlyList<TinhThanhItem> TinhThanhs { get; set; } = Array.Empty<TinhThanhItem>();

    [ValidateNever]
    public IReadOnlyList<PhuongXaItem> PhuongXas { get; set; } = Array.Empty<PhuongXaItem>();

    [ValidateNever]
    public IReadOnlyList<DiaChiNguoiDung> DiaChiDaLuu { get; set; } = Array.Empty<DiaChiNguoiDung>();
}

public sealed class BackupAddressFormVm
{
    [StringLength(60)]
    public string? NhanDiaChi { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
    [StringLength(100)]
    public string TenNguoiNhan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^0[35789][0-9]{8}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string SoDienThoaiNhan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn Tỉnh/Thành phố.")]
    public int? BackupMaTinhThanh { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn Phường/Xã.")]
    public int? BackupMaPhuongXa { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ chi tiết.")]
    [StringLength(300)]
    public string DiaChiChiTiet { get; set; } = string.Empty;

    public bool LaMacDinh { get; set; }
}
