using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Models.ViewModels.Reviews;

namespace SecondHandMarketplace.Web.Models.ViewModels.Home;

/// <summary>Một tab danh mục ở khối "Sản phẩm theo danh mục" trang chủ.</summary>
public sealed record HomeCategoryTabVm(int MaDanhMuc, string TenDanhMuc, IReadOnlyList<ProductCardVm> SanPhams);

/// <summary>
/// Toàn bộ dữ liệu trang chủ. Controller nạp một lần rồi cache, view chỉ việc in —
/// không còn @inject service hay truy vấn nằm trong Razor.
/// </summary>
public class HomeIndexVm
{
    public IReadOnlyList<Banner> Banners { get; init; } = [];
    public IReadOnlyList<DanhMuc> DanhMucs { get; init; } = [];
    public IReadOnlyList<ProductCardVm> SanPhamNoiBat { get; init; } = [];
    public IReadOnlyList<HomeCategoryTabVm> TabsDanhMuc { get; init; } = [];
    public IReadOnlyList<DanhGiaNoiBatVm> DanhGiaNoiBat { get; init; } = [];
}
