namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng GioHang (mỗi người dùng chỉ có một dòng cho mỗi sản phẩm).</summary>
public class GioHang
{
    public int MaGioHang { get; set; }
    public int MaNguoiDung { get; set; }
    public int MaSanPham { get; set; }
    public bool DaChon { get; set; } = true;
    public DateTime ThoiGianThem { get; set; }

    public NguoiDung NguoiDung { get; set; } = null!;
    public SanPham SanPham { get; set; } = null!;
}
