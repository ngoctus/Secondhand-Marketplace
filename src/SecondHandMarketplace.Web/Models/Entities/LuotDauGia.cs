namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng LuotDatGia (mỗi lượt đặt giá của một người trong một phiên).</summary>
public class LuotDatGia
{
    public long MaLuotDatGia { get; set; }
    public int MaPhienDauGia { get; set; }
    public int MaNguoiDung { get; set; }
    public decimal SoTien { get; set; }
    public DateTime ThoiGian { get; set; }

    public PhienDauGia Phien { get; set; } = null!;
    public NguoiDung NguoiDung { get; set; } = null!;
}