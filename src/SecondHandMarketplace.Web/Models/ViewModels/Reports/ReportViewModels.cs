using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.ViewModels.Reports;

/// <summary>Form gửi báo cáo vi phạm.</summary>
public class TaoBaoCaoViewModel
{
    [Required]
    public string LoaiDoiTuong { get; set; } = "PRODUCT"; // PRODUCT, USER, SHOP

    [Required]
    public int MaDoiTuong { get; set; }

    public string? TenDoiTuong { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do báo cáo.")]
    [StringLength(255, ErrorMessage = "Lý do tối đa 255 ký tự.")]
    public string LyDo { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả chi tiết tối đa 1000 ký tự.")]
    public string? MoTa { get; set; }
}

/// <summary>Chi tiết một báo cáo vi phạm.</summary>
public class BaoCaoItemViewModel
{
    public int MaBaoCao { get; set; }
    public string LoaiDoiTuong { get; set; } = string.Empty;
    public int MaDoiTuong { get; set; }
    public string TenDoiTuong { get; set; } = string.Empty;
    public int MaNguoiBaoCao { get; set; }
    public string TenNguoiBaoCao { get; set; } = string.Empty;
    public string LyDo { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public string TrangThai { get; set; } = "PENDING";
    public int? NguoiXuLyId { get; set; }
    public string? TenNguoiXuLy { get; set; }
    public string? GhiChuXuLy { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime? NgayXuLy { get; set; }
}

/// <summary>Danh sách báo cáo quản trị Admin.</summary>
public class AdminReportListViewModel
{
    public string? TrangThaiFilter { get; set; }
    public string? LoaiFilter { get; set; }
    public List<BaoCaoItemViewModel> Items { get; set; } = new();
    public int TongChuaXuLy { get; set; }
}
