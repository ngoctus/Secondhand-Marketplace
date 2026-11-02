namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng TinhTrangSanPham (Mới 100%, Như mới, Tốt...).</summary>
public class TinhTrangSanPham
{
    public int MaTinhTrang { get; set; }
    public string Ma { get; set; } = string.Empty;
    public string Ten { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public int ThuTuHienThi { get; set; }
    public bool DangSuDung { get; set; } = true;
}