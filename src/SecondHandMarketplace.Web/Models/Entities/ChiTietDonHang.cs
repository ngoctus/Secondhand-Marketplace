namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng ChiTietDonHang (lưu lại tên, ảnh, giá tại thời điểm mua).</summary>
public class ChiTietDonHang
{
    public int MaChiTietDonHang { get; set; }
    public int MaDonHang { get; set; }
    public int MaSanPham { get; set; }
    public string TenSanPham { get; set; } = string.Empty;
    public string? AnhDaiDien { get; set; }
    public decimal Gia { get; set; }
    public bool DangGiuCho { get; set; } = true;

    public DonHang DonHang { get; set; } = null!;
    public SanPham SanPham { get; set; } = null!;
}
