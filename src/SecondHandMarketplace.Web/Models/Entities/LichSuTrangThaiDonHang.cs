namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng LichSuTrangThaiDonHang. LoaiNguoiThucHien: BUYER, SELLER, ADMIN, SYSTEM (SYSTEM thì NguoiThucHienId để null).</summary>
public class LichSuTrangThaiDonHang
{
    public int MaLichSuTrangThaiDonHang { get; set; }
    public int MaDonHang { get; set; }
    public string? TrangThaiCu { get; set; }
    public string TrangThaiMoi { get; set; } = string.Empty;
    public string LoaiNguoiThucHien { get; set; } = string.Empty;
    public int? NguoiThucHienId { get; set; }
    public string? LyDo { get; set; }
    public DateTime ThoiGian { get; set; }

    public DonHang DonHang { get; set; } = null!;
}
