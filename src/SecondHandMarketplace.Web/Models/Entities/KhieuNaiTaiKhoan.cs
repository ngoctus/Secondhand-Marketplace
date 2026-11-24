namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>
/// Bảng KhieuNaiTaiKhoan (KN-01): đơn khiếu nại khóa nhầm (APPEAL) hoặc cam kết mở khóa (UNLOCK_REQUEST) của tài khoản bị LOCKED.
/// TrangThai: PENDING | APPROVED | REJECTED. Filtered Unique Index bảo đảm mỗi tài khoản tối đa 1 đơn PENDING.
/// </summary>
public class KhieuNaiTaiKhoan
{
    public int MaKhieuNai { get; set; }
    public int MaNguoiDung { get; set; }
    public string LoaiYeuCau { get; set; } = "APPEAL";
    public int LanKhoaThu { get; set; } = 1;
    public string LyDoKhoaLucGui { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public string? BangChungAnh { get; set; }
    public bool YeuCauPhi { get; set; }
    public decimal SoTienPhi { get; set; }
    public string? MaGiaoDichThamChieu { get; set; }
    public string TrangThai { get; set; } = "PENDING";
    public string? GhiChuAdmin { get; set; }
    public int? NguoiXuLyId { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime? NgayXuLy { get; set; }

    public NguoiDung NguoiDung { get; set; } = null!;
    public NguoiDung? NguoiXuLy { get; set; }
}
