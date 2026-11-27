using SecondHandMarketplace.Web.Models.ViewModels;
using SecondHandMarketplace.Web.Models.ViewModels.Products;

namespace SecondHandMarketplace.Web.Services.Products;

public interface IProductService
{
    /// <summary>Tìm sản phẩm công khai (ACTIVE, chưa xóa, shop và chủ shop đang ACTIVE), có sắp xếp và phân trang.</summary>
    Task<PagedResult<ProductCardVm>> SearchAsync(ProductSearchVm filter);

    /// <summary>Chi tiết sản phẩm công khai (ACTIVE / RESERVED / SOLD). Không có hoặc không được xem thì trả về null.</summary>
    Task<ProductDetailVm?> GetDetailAsync(int id);

    /// <summary>Biên giá thấp nhất/cao nhất của các tin đang bán; (0,0) khi chưa có tin nào. Dùng cho thanh trượt giá (B-04).</summary>
    Task<(decimal Min, decimal Max)> LayBienGiaAsync(CancellationToken cancellationToken = default);

    // ===== B1 – Người bán =====

    /// <summary>Danh sách sản phẩm của người bán (mọi trạng thái, không gồm xóa mềm).</summary>
    Task<PagedResult<MyProductVm>> GetMyAsync(int maNguoiDung, int page = 1, CancellationToken cancellationToken = default);

    /// <summary>Danh sách sản phẩm của người bán kèm lọc (từ khóa, danh mục, ngày tạo, trạng thái) và sắp xếp.</summary>
    async Task<(PagedResult<MyProductVm> Result, int TotalAll)> GetMyFilteredAsync(
        int maNguoiDung, SellerProductFilterVm filter, CancellationToken cancellationToken = default)
    {
        var paged = await GetMyAsync(maNguoiDung, filter.Page, cancellationToken);
        return (paged, paged.TotalItems);
    }

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

    /// <summary>Ẩn sản phẩm do gửi duyệt thất bại (không cho mở ẩn trực tiếp để tránh lách duyệt).</summary>
    Task<(bool Success, string? Error)> HideDueToFailedReviewAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default)
        => HideAsync(maNguoiDung, maSanPham, cancellationToken);

    /// <summary>Mở ẩn sản phẩm (HIDDEN → ACTIVE) không cần duyệt lại nếu tin chỉ bị ẩn mà chưa chỉnh sửa.</summary>
    Task<(bool Success, string? Error)> UnhideAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default)
        => Task.FromResult<(bool, string?)>((false, "Không hỗ trợ mở ẩn trực tiếp."));

    /// <summary>Gửi duyệt qua sp_GuiDuyetSanPham. Khi rule fail, Error chứa các lỗi gộp từ procedure.</summary>
    Task<(bool Success, string? Error)> GuiDuyetAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default);

    /// <summary>Xóa mềm sản phẩm qua sp_XoaMemSanPham (không xóa được khi đã bán hoặc có đơn hàng).</summary>
    Task<(bool Success, string? Error)> DeleteAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default);

    /// <summary>Đặt một ảnh làm ảnh chính (unique theo sản phẩm).</summary>
    Task<(bool Success, string? Error)> DatAnhChinhAsync(int maNguoiDung, int maSanPham, int maAnh, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa một ảnh của sản phẩm; nếu là ảnh chính, ảnh còn lại có ThuTu nhỏ nhất được thăng làm chính.
    /// MaAnhChinhMoi trả về để lưới ảnh (B-03) tô đúng ảnh, vì thứ tự hiển thị có thể khác thứ tự ThuTu.
    /// </summary>
    Task<(bool Success, string? Error, int? MaAnhChinhMoi)> XoaAnhAsync(int maNguoiDung, int maSanPham, int maAnh, CancellationToken cancellationToken = default);
}