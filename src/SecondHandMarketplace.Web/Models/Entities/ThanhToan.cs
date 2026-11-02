namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng ThanhToan. TrangThaiThanhToan: PENDING, SUCCESS, FAILED, REFUNDED.</summary>
public class ThanhToan
{
    public int MaThanhToan { get; set; }
    public int MaDonHang { get; set; }
    public string? MaGiaoDich { get; set; }
    public string PhuongThucThanhToan { get; set; } = string.Empty;
    public decimal SoTien { get; set; }
    public string TrangThaiThanhToan { get; set; } = "PENDING";
    public DateTime? ThoiGianThanhToan { get; set; }
    public DateTime NgayTao { get; set; }

    public DonHang DonHang { get; set; } = null!;
}
