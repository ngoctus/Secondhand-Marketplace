using SecondHandMarketplace.Web.Models.ViewModels;
using SecondHandMarketplace.Web.Models.ViewModels.Products;

namespace SecondHandMarketplace.Web.Services.Favorites;

/// <summary>Yêu thích sản phẩm (B4).</summary>
public interface IFavoriteService
{
    /// <summary>Bật/tắt yêu thích. Trả về trạng thái sau khi thao tác; idempotent với va chạm UNIQUE.</summary>
    Task<(bool ThanhCong, bool LaYeuThich)> ToggleAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default);

    /// <summary>Danh sách sản phẩm đang yêu thích (kể cả sản phẩm đã ẩn/từ chối — hiển thị nguyên trạng).</summary>
    Task<PagedResult<ProductCardVm>> LayDanhSachAsync(int maNguoiDung, int page = 1, CancellationToken cancellationToken = default);

    /// <summary>Tập MaSanPham người dùng đang yêu thích, để đánh dấu nút tim trên lưới sản phẩm.</summary>
    Task<HashSet<int>> LayIdsDangYeuThichAsync(int maNguoiDung, CancellationToken cancellationToken = default);
}
