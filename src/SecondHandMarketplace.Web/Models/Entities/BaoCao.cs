namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>
/// Bảng BaoCao: báo cáo vi phạm sản phẩm, người dùng hoặc cửa hàng.
/// LoaiDoiTuong: PRODUCT, USER, SHOP.
/// TrangThai: PENDING, RESOLVED, DISMISSED.
/// </summary>
public class BaoCao
{
    public int MaBaoCao { get; set; }
    public string LoaiDoiTuong { get; set; } = "PRODUCT";
    public int MaDoiTuong { get; set; }
    public int MaNguoiBaoCao { get; set; }
    public string LyDo { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public string TrangThai { get; set; } = "PENDING";
    public int? NguoiXuLyId { get; set; }
    public string? GhiChuXuLy { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime? NgayXuLy { get; set; }

    public NguoiDung NguoiBaoCao { get; set; } = null!;
    public NguoiDung? NguoiXuLy { get; set; }
}
