using System.ComponentModel.DataAnnotations;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels;

namespace SecondHandMarketplace.Web.Models.ViewModels.Products;

/// <summary>Một sản phẩm trong màn "Sản phẩm của tôi" của người bán (B1).</summary>
public class MyProductVm
{
    public int Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public decimal? Gia { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public int? MaDanhMuc { get; set; }
    public string? TenDanhMuc { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }
    public string? AnhDaiDien { get; set; }
    public int SoLuongAnh { get; set; }
    public string? LyDoTuChoiGanNhat { get; set; }

    /// <summary>Lượt đang mở có PhanTichAI = FAILED (lỗi hệ thống) → cho phép gửi duyệt/phân tích lại.</summary>
    public bool PhanTichThatBai { get; set; }

    /// <summary>Tin đang HIDDEN từ ACTIVE và chưa chỉnh sửa kể từ lúc ẩn → mở bán lại không cần duyệt.</summary>
    public bool CoTheMoAnNgay { get; set; }
}

/// <summary>Bộ lọc và sắp xếp trong trang Quản lý sản phẩm của người bán.</summary>
public class SellerProductFilterVm
{
    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public DateOnly? CreatedDate { get; set; }
    public string? Status { get; set; }
    public string? Sort { get; set; } = "updated_desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;

    public bool CoBoLoc =>
        !string.IsNullOrWhiteSpace(Keyword)
        || CategoryId.HasValue
        || CreatedDate.HasValue
        || !string.IsNullOrWhiteSpace(Status);

    public static readonly string[] DanhSachTrangThai =
    [
        "ACTIVE",
        "PENDING_REVIEW",
        "REJECTED",
        "HIDDEN",
        "DRAFT",
        "RESERVED",
        "SOLD",
        "REMOVED"
    ];

    public static readonly int[] CacMucHienThi = [12, 24, 48];
}

/// <summary>Dữ liệu màn Quản lý sản phẩm của người bán (kèm bộ lọc, danh mục và tổng số tin của shop).</summary>
public class SellerProductListVm
{
    public SellerProductFilterVm Filter { get; init; } = new();
    public PagedResult<MyProductVm> Result { get; init; } = new();
    public IReadOnlyList<DanhMuc> DanhSachDanhMuc { get; init; } = [];
    public int TongTatCaSanPham { get; init; }
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
    [Required(ErrorMessage = "Giá sản phẩm là bắt buộc.")]
    [Range(1, 99_999_999_999, ErrorMessage = "Giá phải lớn hơn 0.")]
    public decimal? Gia { get; set; }

    [Display(Name = "Danh mục")]
    [Required(ErrorMessage = "Vui lòng chọn danh mục sản phẩm.")]
    public int? MaDanhMuc { get; set; }

    [Display(Name = "Tình trạng")]
    [Required(ErrorMessage = "Vui lòng chọn tình trạng sản phẩm.")]
    public int? MaTinhTrang { get; set; }

    public List<DanhMuc> DanhSachDanhMuc { get; set; } = new();
    public List<TinhTrangSanPham> DanhSachTinhTrang { get; set; } = new();
    public List<ProductImageVm> AnhHienTai { get; set; } = new();

    /// <summary>Trạng thái hiện tại của sản phẩm (chỉ để đọc, không nhận từ form POST).</summary>
    public string TrangThai { get; set; } = string.Empty;

    /// <summary>True nếu tin đang HIDDEN từ ACTIVE và chưa chỉnh sửa sau khi ẩn.</summary>
    public bool CoTheMoAnNgay { get; set; }
}
