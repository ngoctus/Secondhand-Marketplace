using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Services.Areas;

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

    // ---- Địa chỉ / khu vực (bắt buộc; shop cũ có cột NULL vẫn mở được form nhưng phải bổ sung khi lưu) ----
    [Required(ErrorMessage = "Chọn tỉnh/thành phố")]
    [Display(Name = "Tỉnh/Thành phố")]
    public int? MaTinhThanh { get; set; }

    [Required(ErrorMessage = "Chọn phường/xã")]
    [Display(Name = "Phường/Xã")]
    public int? MaPhuongXa { get; set; }

    [Required(ErrorMessage = "Nhập địa chỉ chi tiết"), StringLength(300, ErrorMessage = "Địa chỉ chi tiết tối đa 300 ký tự")]
    [Display(Name = "Địa chỉ chi tiết (số nhà, đường, thôn/xóm)")]
    public string? DiaChiChiTiet { get; set; }

    [StringLength(100, ErrorMessage = "Tên khu vực tối đa 100 ký tự")]
    [Display(Name = "Tên khu vực quen thuộc (không bắt buộc)")]
    public string? KhuVucHienThi { get; set; }

    // ---- Thông tin nhận chuyển khoản (tùy chọn; đã nhập thì phải đủ ngân hàng, số tài khoản, tên chủ tài khoản) ----
    [StringLength(10)]
    [Display(Name = "Ngân hàng")]
    public string? MaNganHang { get; set; }

    /// <summary>Tên ngân hàng hiển thị, do server điền theo MaNganHang.</summary>
    public string? NganHang { get; set; }

    [StringLength(30, ErrorMessage = "Số tài khoản tối đa 30 chữ số")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "Số tài khoản chỉ gồm chữ số")]
    [Display(Name = "Số tài khoản")]
    public string? SoTaiKhoan { get; set; }

    [StringLength(100, ErrorMessage = "Tên chủ tài khoản tối đa 100 ký tự")]
    [Display(Name = "Tên chủ tài khoản")]
    public string? TenChuTaiKhoan { get; set; }

    // ---- Dữ liệu nạp cho dropdown, không nhận từ form ----
    [ValidateNever]
    public IReadOnlyList<TinhThanhItem> TinhThanhs { get; set; } = Array.Empty<TinhThanhItem>();

    [ValidateNever]
    public IReadOnlyList<PhuongXaItem> PhuongXas { get; set; } = Array.Empty<PhuongXaItem>();

    [ValidateNever]
    public IReadOnlyList<NganHangVietQr> NganHangs { get; set; } = VietQrBanks.All;
}
