namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng HoiThoai: một cuộc trò chuyện 1-1. MaNguoiDung1 luôn nhỏ hơn MaNguoiDung2.</summary>
public class HoiThoai
{
    public int MaHoiThoai { get; set; }
    public int MaNguoiDung1 { get; set; }
    public int MaNguoiDung2 { get; set; }
    public string? NoiDungCuoi { get; set; }
    public DateTime? NgayTinCuoi { get; set; }
    public DateTime NgayTao { get; set; }

    public NguoiDung NguoiDung1 { get; set; } = null!;
    public NguoiDung NguoiDung2 { get; set; } = null!;
    public List<TinNhan> TinNhans { get; set; } = new();
}