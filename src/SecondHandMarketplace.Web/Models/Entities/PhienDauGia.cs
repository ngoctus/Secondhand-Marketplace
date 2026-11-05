namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng PhienDauGia. TrangThai: SCHEDULED, RUNNING, ENDED, CANCELLED. NguoiHuy: SELLER, ADMIN, SYSTEM.</summary>
public class PhienDauGia
{
    public int MaPhienDauGia { get; set; }
    public int MaSanPham { get; set; }
    public decimal GiaNiemYet { get; set; }
    public decimal GiaKhoiDiem { get; set; }
    public decimal BuocGia { get; set; }
    public decimal GiaHienTai { get; set; }
    public int SoLuotDat { get; set; }
    public int? MaNguoiDanDau { get; set; }
    public DateTime ThoiGianBatDau { get; set; }
    public DateTime ThoiGianKetThuc { get; set; }
    public string TrangThai { get; set; } = "SCHEDULED";
    public int? MaNguoiThang { get; set; }
    public decimal? GiaThang { get; set; }
    public int? MaDonHang { get; set; }
    public string? NguoiHuy { get; set; }
    public string? LyDoHuy { get; set; }
    public DateTime? ThoiGianHuy { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }

    public SanPham SanPham { get; set; } = null!;
    public NguoiDung? NguoiDanDau { get; set; }
    public NguoiDung? NguoiThang { get; set; }
    public List<LuotDatGia> LuotDat { get; set; } = new();
}