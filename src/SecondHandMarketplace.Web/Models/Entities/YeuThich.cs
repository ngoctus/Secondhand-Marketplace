namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng YeuThich (mỗi người yêu thích một sản phẩm đúng 1 lần).</summary>
public class YeuThich
{
    public int MaYeuThich { get; set; }
    public int MaNguoiDung { get; set; }
    public int MaSanPham { get; set; }
    public DateTime NgayTao { get; set; }

    public NguoiDung NguoiDung { get; set; } = null!;
    public SanPham SanPham { get; set; } = null!;
}
