namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng TinNhan. DaDoc = người nhận đã xem.</summary>
public class TinNhan
{
    public long MaTinNhan { get; set; }
    public int MaHoiThoai { get; set; }
    public int MaNguoiGui { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public bool DaDoc { get; set; }
    public DateTime NgayGui { get; set; }

    public HoiThoai HoiThoai { get; set; } = null!;
    public NguoiDung NguoiGui { get; set; } = null!;
}