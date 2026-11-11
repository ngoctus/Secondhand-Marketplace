using System.ComponentModel.DataAnnotations;

using SecondHandMarketplace.Web.Models.ViewModels.Cart;

namespace SecondHandMarketplace.Web.Models.ViewModels.Orders;

/// <summary>Dữ liệu màn hình Thanh toán & Đặt hàng.</summary>
public class CheckoutViewModel

{
    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
    [StringLength(100)]
    public string TenNguoiNhan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại nhận hàng.")]
    [RegularExpression(@"^[0-9]{9,15}$", ErrorMessage = "Số điện thoại từ 9 đến 15 chữ số.")]
    public string SoDienThoaiNhan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng.")]
    [StringLength(500)]
    public string DiaChiNhan { get; set; } = string.Empty;

    public string PhuongThucThanhToan { get; set; } = "COD"; // COD, BANK_TRANSFER, ONLINE

    /// <summary>
    /// Hình thức nhận: GIAO_HANG (Giao hàng tận nơi) hoặc GAP_TRUC_TIEP (Gặp mặt trực tiếp).
    /// </summary>
    public string HinhThucNhan { get; set; } = "GIAO_HANG";

    public int? MaSanPhamMuaNgay { get; set; }

    public CartIndexViewModel? GioHang { get; set; }

    public CartItemViewModel? SanPhamMuaNgay { get; set; }

    public decimal TienHang { get; set; }

    [Range(0, 10000000, ErrorMessage = "Phí vận chuyển phải từ 0đ trở lên.")]
    public decimal PhiVanChuyen { get; set; }

    public decimal TongThanhToan => TienHang + PhiVanChuyen;

}

/// <summary>Kết quả sau khi đặt hàng.</summary>
public class DatHangResultViewModel

{
    public bool ThanhCong { get; set; }

    public string? ThongBao { get; set; }

    public List<string> DanhSachMaDon { get; set; } = new();

}

/// <summary>Tổng quan một đơn hàng trong danh sách đơn mua.</summary>
public class DonHangItemViewModel

{
    public int MaDonHang { get; set; }

    public string MaDon { get; set; } = string.Empty;

    public int MaCuaHang { get; set; }

    public string TenShop { get; set; } = string.Empty;

    public decimal TienHang { get; set; }

    public decimal PhiVanChuyen { get; set; }

    public decimal TongTien { get; set; }

    public string TrangThai { get; set; } = "PENDING";

    public string TrangThaiThanhToan { get; set; } = "UNPAID";

    public string PhuongThucThanhToan { get; set; } = "COD";

    public string HinhThucNhan { get; set; } = "GIAO_HANG";

    public DateTime NgayTao { get; set; }

    public string? NguoiHuy { get; set; }

    public string? LyDoHuy { get; set; }

    public List<ChiTietDonHangItemViewModel> ChiTiet { get; set; } = new();

}

/// <summary>Một món hàng trong đơn.</summary>
public class ChiTietDonHangItemViewModel

{
    public int MaChiTietDonHang { get; set; }

    public int MaSanPham { get; set; }

    public string TenSanPham { get; set; } = string.Empty;

    public string? AnhDaiDien { get; set; }

    public decimal Gia { get; set; }

}

/// <summary>Chi tiết đầy đủ đơn hàng phía người mua.</summary>
public class DonHangChiTietViewModel

{
    public int MaDonHang { get; set; }

    public string MaDon { get; set; } = string.Empty;

    public int MaCuaHang { get; set; }

    public string TenShop { get; set; } = string.Empty;

    public string TenNguoiNhan { get; set; } = string.Empty;

    public string SoDienThoaiNhan { get; set; } = string.Empty;

    public string DiaChiNhan { get; set; } = string.Empty;

    public decimal TienHang { get; set; }

    public decimal PhiVanChuyen { get; set; }

    public decimal TongTien { get; set; }

    public string PhuongThucThanhToan { get; set; } = "COD";

    public string HinhThucNhan { get; set; } = "GIAO_HANG";

    public string TrangThaiThanhToan { get; set; } = "UNPAID";

    public string TrangThai { get; set; } = "PENDING";

    public DateTime NgayTao { get; set; }

    public DateTime? ThoiGianXacNhan { get; set; }

    public DateTime? ThoiGianBatDauGiao { get; set; }

    public DateTime? ThoiGianDaGiao { get; set; }

    public DateTime? ThoiGianHoanTat { get; set; }

    public string? NguoiHuy { get; set; }

    public string? LyDoHuy { get; set; }

    public DateTime? ThoiGianHuy { get; set; }

    public List<ChiTietDonHangItemViewModel> DanhSachSanPham { get; set; } = new();

    public List<LichSuTrangThaiDonViewModel> LichSuTrangThai { get; set; } = new();

    public int? MaTranhChap { get; set; }

    public bool CoTheHuy => SecondHandMarketplace.Web.Common.OrderRules.NguoiMuaDuocHuy(TrangThai);

    public bool CoTheXacNhanNhanHang => TrangThai == "DELIVERED" || (HinhThucNhan == "GAP_TRUC_TIEP" && TrangThai == "SHIPPING");

    public bool CoTheMoTranhChap => TrangThai == "SHIPPING" || TrangThai == "DELIVERED" || TrangThai == "COMPLETED";

}

/// <summary>Dòng lịch sử thay đổi trạng thái đơn.</summary>
public class LichSuTrangThaiDonViewModel

{
    public string? TrangThaiCu { get; set; }

    public string TrangThaiMoi { get; set; } = string.Empty;

    public string LoaiNguoiThucHien { get; set; } = string.Empty;

    public string? LyDo { get; set; }

    public DateTime ThoiGian { get; set; }

}

