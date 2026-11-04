namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>
/// Bảng TranhChap: khiếu nại giữa người mua và shop về đơn hàng.
/// TrangThai: OPEN, PROCESSING, RESOLVED, REJECTED.
/// </summary>
public class TranhChap
{
    public int MaTranhChap { get; set; }
    public int MaDonHang { get; set; }
    public int MaNguoiTao { get; set; }
    public string LyDo { get; set; } = string.Empty;
    public string? BangChungAnh { get; set; }
    public string? PhanHoiNguoiBan { get; set; }
    public string TrangThai { get; set; } = "OPEN";
    public string? PhanQuyet { get; set; }
    public string? GhiChuAdmin { get; set; }
    public int? NguoiXuLyId { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }
    public DateTime? NgayGiaiQuyet { get; set; }

    public DonHang DonHang { get; set; } = null!;
    public NguoiDung NguoiTao { get; set; } = null!;
    public NguoiDung? NguoiXuLy { get; set; }
}
