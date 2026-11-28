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
    /// <summary>true = từ chối có ghi nhận 1 gậy vi phạm; false = từ chối nhẹ yêu cầu chỉnh sửa (không đánh gậy).</summary>
    public bool GhiNhanViPham { get; set; } = true;

    public SanPham SanPham { get; set; } = null!;
    public PhanTichAI? PhanTich { get; set; }
}
