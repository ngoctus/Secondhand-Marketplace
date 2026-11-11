namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng TinhThanh: 34 tỉnh/thành (không có cấp quận/huyện).</summary>
public class TinhThanh
{
    public int MaTinhThanh { get; set; }
    public string TenTinhThanh { get; set; } = string.Empty;
    public string MaHanhChinh { get; set; } = string.Empty;

    public ICollection<PhuongXa> PhuongXas { get; set; } = new List<PhuongXa>();
}
