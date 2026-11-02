namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>
/// Bảng SanPham. TrangThai là chữ hoa đúng như DB:
/// DRAFT, PENDING_REVIEW, REJECTED, ACTIVE, HIDDEN, RESERVED, SOLD, REMOVED.
/// </summary>
public class SanPham
{
    public int MaSanPham { get; set; }
    public int MaCuaHang { get; set; }
    public int? MaDanhMuc { get; set; }
    public int? MaTinhTrang { get; set; }
    public string Ten { get; set; } = string.Empty;
    public string ChuoiTimKiem { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public decimal? Gia { get; set; }
    public string TrangThai { get; set; } = "DRAFT";
    public string? LyDoGo { get; set; }
    public bool DaXoa { get; set; }
    public DateTime? ThoiGianXoa { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }

    public CuaHang CuaHang { get; set; } = null!;
    public DanhMuc? DanhMuc { get; set; }
    public TinhTrangSanPham? TinhTrang { get; set; }
    public List<AnhSanPham> Anh { get; set; } = new();
}