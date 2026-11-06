using SecondHandMarketplace.Web.Models.ViewModels;
using SecondHandMarketplace.Web.Models.ViewModels.Products;

namespace SecondHandMarketplace.Web.Services.Products;

public interface IProductService
{
    /// <summary>Tìm sản phẩm công khai (ACTIVE, chưa xóa, shop và chủ shop đang ACTIVE).</summary>
    Task<List<ProductCardVm>> SearchAsync(ProductSearchVm filter);

    /// <summary>Chi tiết sản phẩm công khai (ACTIVE / RESERVED / SOLD). Không có hoặc không được xem thì trả về null.</summary>
    Task<ProductDetailVm?> GetDetailAsync(int id);

    // ===== B1 – Người bán =====

    /// <summary>Danh sách sản phẩm của người bán (mọi trạng thái, không gồm xóa mềm).</summary>
    Task<PagedResult<MyProductVm>> GetMyAsync(int maNguoiDung, int page = 1, CancellationToken cancellationToken = default);

    /// <summary>Nạp form sửa sản phẩm của chính người bán; null nếu không tồn tại hoặc không thuộc về họ.</summary>
    Task<ProductFormVm?> GetFormAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default);

    /// <summary>Tạo sản phẩm DRAFT kèm ảnh (tối đa 10, ảnh đầu là ảnh chính). Trả về MaSanPham mới.</summary>
    Task<(bool Success, string? Error, int Id)> CreateAsync(
        int maNguoiDung, ProductFormVm form, IReadOnlyList<ProductImageInput> anh,
        CancellationToken cancellationToken = default);

    /// <summary>Sửa sản phẩm của mình; không sửa được khi đang PENDING_REVIEW.</summary>
    Task<(bool Success, string? Error)> UpdateAsync(
        int maNguoiDung, int maSanPham, ProductFormVm form, IReadOnlyList<ProductImageInput> anhMoi,
        CancellationToken cancellationToken = default);

    /// <summary>Ẩn sản phẩm đang bán (ACTIVE → HIDDEN) qua sp_ChuyenTrangThaiSanPham.</summary>
    Task<(bool Success, string? Error)> HideAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default);

    /// <summary>Gửi duyệt qua sp_GuiDuyetSanPham. Khi rule fail, Error chứa các lỗi gộp từ procedure.</summary>
    Task<(bool Success, string? Error)> GuiDuyetAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default);

    /// <summary>Đặt một ảnh làm ảnh chính (unique theo sản phẩm).</summary>
    Task<(bool Success, string? Error)> DatAnhChinhAsync(int maNguoiDung, int maSanPham, int maAnh, CancellationToken cancellationToken = default);

    /// <summary>Xóa một ảnh của sản phẩm; nếu là ảnh chính, ảnh còn lại đầu tiên được thăng làm chính.</summary>
    Task<(bool Success, string? Error)> XoaAnhAsync(int maNguoiDung, int maSanPham, int maAnh, CancellationToken cancellationToken = default);
}