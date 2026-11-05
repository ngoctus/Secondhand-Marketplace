namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng LuotKiemDuyet (mỗi lần người bán gửi duyệt là một lượt). QuyetDinh: APPROVED, REJECTED.</summary>
public class LuotKiemDuyet
{
    public int MaLuotKiemDuyet { get; set; }
    public int MaSanPham { get; set; }
    public DateTime ThoiGianGui { get; set; }
    public string? KetQuaRule { get; set; }      // JSON
    public string? QuyetDinh { get; set; }
    public string? LyDo { get; set; }
    public int? MaQuanTriVien { get; set; }
    public DateTime? ThoiGianQuyetDinh { get; set; }
    /// <summary>true = hệ thống tự từ chối (HIGH, không có Admin). Cột thêm bởi database/15_luot_kiem_duyet_tu_choi_he_thong_c2.sql.</summary>
    public bool QuyetDinhTuDong { get; set; }

    public SanPham SanPham { get; set; } = null!;
    public PhanTichAI? PhanTich { get; set; }
}
