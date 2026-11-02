namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng ThongBao. Loai: ORDER, PRODUCT, SYSTEM. MaThamChieu là mã đơn hàng hoặc sản phẩm tương ứng.</summary>
public class ThongBao
{
    public int MaThongBao { get; set; }
    public int MaNguoiDung { get; set; }
    public string Loai { get; set; } = string.Empty;
    public string TieuDe { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public int? MaThamChieu { get; set; }
    public bool DaDoc { get; set; }
    public DateTime NgayTao { get; set; }
}
