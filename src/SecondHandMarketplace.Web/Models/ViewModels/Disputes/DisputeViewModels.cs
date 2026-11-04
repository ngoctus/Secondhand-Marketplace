using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.ViewModels.Disputes;

/// <summary>Form người mua mở khiếu nại / tranh chấp cho đơn hàng.</summary>
public class MoTranhChapViewModel
{
    [Required]
    public int MaDonHang { get; set; }

    public string? MaDon { get; set; }
    public string? TenShop { get; set; }
    public decimal TongTien { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do mở tranh chấp.")]
    [StringLength(500, ErrorMessage = "Lý do tối đa 500 ký tự.")]
    public string LyDo { get; set; } = string.Empty;

    [StringLength(500)]
    public string? BangChungAnh { get; set; }
}

/// <summary>Chi tiết một tranh chấp đơn hàng.</summary>
public class TranhChapDetailViewModel
{
    public int MaTranhChap { get; set; }
    public int MaDonHang { get; set; }
    public string MaDon { get; set; } = string.Empty;
    public int MaNguoiTao { get; set; }
    public string TenNguoiTao { get; set; } = string.Empty;
    public string TenShop { get; set; } = string.Empty;
    public decimal TongTien { get; set; }
    public string TrangThaiDonHang { get; set; } = string.Empty;
    public string LyDo { get; set; } = string.Empty;
    public string? BangChungAnh { get; set; }
    public string? PhanHoiNguoiBan { get; set; }
    public string TrangThai { get; set; } = "OPEN";
    public string? PhanQuyet { get; set; }
    public string? GhiChuAdmin { get; set; }
    public int? NguoiXuLyId { get; set; }
    public string? TenNguoiXuLy { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }
    public DateTime? NgayGiaiQuyet { get; set; }
}

/// <summary>Danh sách tranh chấp quản trị Admin.</summary>
public class AdminDisputeListViewModel
{
    public string? TrangThaiFilter { get; set; }
    public List<TranhChapDetailViewModel> Items { get; set; } = new();
    public int TongChuaGiaiQuyet { get; set; }
}
