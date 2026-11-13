using System.ComponentModel.DataAnnotations;
using SecondHandMarketplace.Web.Models.Entities;

namespace SecondHandMarketplace.Web.Models.ViewModels.Products;

/// <summary>Một sản phẩm trong màn "Sản phẩm của tôi" của người bán (B1).</summary>
public class MyProductVm
{
    public int Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public decimal? Gia { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }
    public string? AnhDaiDien { get; set; }
    public int SoLuongAnh { get; set; }
    public string? LyDoTuChoiGanNhat { get; set; }

    /// <summary>Lượt đang mở có PhanTichAI = FAILED (lỗi hệ thống) → cho phép gửi duyệt/phân tích lại.</summary>
    public bool PhanTichThatBai { get; set; }
}

/// <summary>Ảnh đã có của sản phẩm, dùng trong form sửa (B1).</summary>
public class ProductImageVm
{
    public int MaAnh { get; set; }
    public string DuongDan { get; set; } = string.Empty;
    public bool LaAnhChinh { get; set; }
}

/// <summary>Ảnh mới đã lưu lên đĩa, service chỉ nhận đường dẫn + dung lượng.</summary>
public sealed record ProductImageInput(string DuongDan, int DungLuong);

/// <summary>Form đăng bán / sửa sản phẩm (B1).</summary>
public class ProductFormVm
{
    public int? Id { get; set; }

    [Display(Name = "Tên sản phẩm")]
    [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Tên phải từ 5 đến 200 ký tự.")]
    public string Ten { get; set; } = string.Empty;

    [Display(Name = "Mô tả")]
    [Required(ErrorMessage = "Mô tả là bắt buộc.")]
    [MinLength(20, ErrorMessage = "Mô tả cần ít nhất 20 ký tự để người mua hiểu tình trạng món đồ.")]
    [StringLength(4000, ErrorMessage = "Mô tả tối đa 4000 ký tự.")]
    public string? MoTa { get; set; }

    [Display(Name = "Giá (đồng)")]
    [Range(1, 99_999_999_999, ErrorMessage = "Giá phải lớn hơn 0.")]
    public decimal? Gia { get; set; }

    [Display(Name = "Danh mục")]
    public int? MaDanhMuc { get; set; }

    [Display(Name = "Tình trạng")]
    public int? MaTinhTrang { get; set; }

    public List<DanhMuc> DanhSachDanhMuc { get; set; } = new();
    public List<TinhTrangSanPham> DanhSachTinhTrang { get; set; } = new();
    public List<ProductImageVm> AnhHienTai { get; set; } = new();

    /// <summary>Trạng thái hiện tại của sản phẩm (chỉ để đọc, không nhận từ form POST).</summary>
    public string TrangThai { get; set; } = string.Empty;
}
