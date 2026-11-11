namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng PhuongXa: xã/phường thuộc một tỉnh/thành.</summary>
public class PhuongXa
{
    public int MaPhuongXa { get; set; }
    public int MaTinhThanh { get; set; }
    public string TenPhuongXa { get; set; } = string.Empty;
    public string MaHanhChinh { get; set; } = string.Empty;

    public TinhThanh TinhThanh { get; set; } = null!;
}
