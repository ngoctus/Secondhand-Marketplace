namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Đánh giá của người mua dành cho cửa hàng sau khi hoàn tất đơn.</summary>
public class DanhGia
{
    public int MaDanhGia { get; set; }
    public int MaDonHang { get; set; }
    public int MaChiTietDonHang { get; set; }
    public int MaNguoiMua { get; set; }
    public int MaCuaHang { get; set; }
    public int SoSao { get; set; }
    public string? NoiDung { get; set; }
    public string TrangThai { get; set; } = "PENDING";
    public int? MaAdminXuLy { get; set; }
    public string? GhiChuAdmin { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime? NgayDuyet { get; set; }

    public DonHang DonHang { get; set; } = null!;
    public ChiTietDonHang ChiTietDonHang { get; set; } = null!;
    public NguoiDung NguoiMua { get; set; } = null!;
    public CuaHang CuaHang { get; set; } = null!;
}
