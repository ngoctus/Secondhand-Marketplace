namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng LichSuTrangThaiSanPham. LoaiNguoiThucHien: BUYER, SELLER, ADMIN, SYSTEM (SYSTEM thì NguoiThucHienId để null).</summary>
public class LichSuTrangThaiSanPham
{
    public int MaLichSuTrangThaiSanPham { get; set; }
    public int MaSanPham { get; set; }
    public string? TrangThaiCu { get; set; }
    public string TrangThaiMoi { get; set; } = string.Empty;
    public string LoaiNguoiThucHien { get; set; } = string.Empty;
    public int? NguoiThucHienId { get; set; }
    public string? LyDo { get; set; }
    public DateTime ThoiGian { get; set; }
}
