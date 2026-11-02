namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng AnhSanPham (tối đa 10 ảnh / sản phẩm, đúng 1 ảnh chính).</summary>
public class AnhSanPham
{
    public int MaAnhSP { get; set; }
    public int MaSanPham { get; set; }
    public string DuongDan { get; set; } = string.Empty;
    public int ThuTu { get; set; }
    public bool LaAnhChinh { get; set; }
    public int DungLuong { get; set; }
    public DateTime NgayTao { get; set; }

    public SanPham SanPham { get; set; } = null!;
}