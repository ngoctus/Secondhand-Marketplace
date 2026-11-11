namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>
/// Bảng DonHang (mỗi đơn thuộc đúng một cửa hàng).
/// TrangThai: PENDING, CONFIRMED, SHIPPING, DELIVERED, COMPLETED, CANCELLED.
/// PhuongThucThanhToan: COD, BANK_TRANSFER, ONLINE.
/// TrangThaiThanhToan: UNPAID, PENDING, PAID, REFUNDED, FAILED.
/// HinhThucNhan: GIAO_HANG (Giao hàng tận nơi), GAP_TRUC_TIEP (Gặp mặt trực tiếp).
/// NguoiHuy: BUYER, SELLER, ADMIN, SYSTEM.
/// </summary>
public class DonHang
{
    public int MaDonHang { get; set; }
    public string MaDon { get; set; } = string.Empty;
    public int MaNguoiDung { get; set; }
    public int MaCuaHang { get; set; }
    public string TenNguoiNhan { get; set; } = string.Empty;
    public string SoDienThoaiNhan { get; set; } = string.Empty;
    public string DiaChiNhan { get; set; } = string.Empty;
    public decimal TienHang { get; set; }
    public decimal PhiVanChuyen { get; set; }
    public decimal TongTien { get; set; }
    public string PhuongThucThanhToan { get; set; } = "COD";
    public string TrangThaiThanhToan { get; set; } = "UNPAID";
    public string TrangThai { get; set; } = "PENDING";
    public string HinhThucNhan { get; set; } = "GIAO_HANG";
    public string? NguoiHuy { get; set; }
    public string? LyDoHuy { get; set; }
    public DateTime? ThoiGianHuy { get; set; }
    public DateTime? ThoiGianXacNhan { get; set; }
    public DateTime? ThoiGianBatDauGiao { get; set; }
    public DateTime? ThoiGianDaGiao { get; set; }
    public DateTime? ThoiGianHoanTat { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }

    /// <summary>Người mua.</summary>
    public NguoiDung NguoiDung { get; set; } = null!;
    public CuaHang CuaHang { get; set; } = null!;
    public List<ChiTietDonHang> ChiTiet { get; set; } = new();
    public List<LichSuTrangThaiDonHang> LichSu { get; set; } = new();
    public List<ThanhToan> ThanhToans { get; set; } = new();
}
