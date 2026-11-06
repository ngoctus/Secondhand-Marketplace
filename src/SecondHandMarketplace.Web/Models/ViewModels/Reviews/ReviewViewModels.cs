using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.ViewModels.Reviews;

public class TaoDanhGiaViewModel
{
    [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5.")]
    public int SoSao { get; set; } = 5;

    [StringLength(1000, ErrorMessage = "Nội dung tối đa 1000 ký tự.")]
    public string? NoiDung { get; set; }

    [Required]
    public int MaChiTietDonHang { get; set; }
}

public class DanhGiaChoPhepViewModel
{
    public int MaChiTietDonHang { get; set; }
    public int MaDonHang { get; set; }
    public string MaDon { get; set; } = string.Empty;
    public string TenSanPham { get; set; } = string.Empty;
    public string TenShop { get; set; } = string.Empty;
    public DateTime NgayHoanTat { get; set; }
}

public class UserReviewListViewModel
{
    public List<DanhGiaChoPhepViewModel> ChoDanhGia { get; set; } = new();
}

public class AdminReviewItemViewModel
{
    public int MaDanhGia { get; set; }
    public string MaDon { get; set; } = string.Empty;
    public string TenSanPham { get; set; } = string.Empty;
    public string TenNguoiMua { get; set; } = string.Empty;
    public string TenShop { get; set; } = string.Empty;
    public int SoSao { get; set; }
    public string? NoiDung { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; }
    public string? GhiChuAdmin { get; set; }
}

public class AdminReviewListViewModel
{
    public List<AdminReviewItemViewModel> Items { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
