using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels;

namespace SecondHandMarketplace.Web.Models.ViewModels.Products;

/// <summary>Bộ lọc tìm kiếm sản phẩm (query string: /Products?Keyword=iphone&amp;PriceRange=2&amp;sort=price_asc&amp;page=2)</summary>
public class ProductSearchVm
{
    public string? Keyword { get; set; }          // tên sản phẩm hoặc tên cửa hàng
    public int? CategoryId { get; set; }          // DanhMuc.MaDanhMuc
    public int? ConditionId { get; set; }         // TinhTrangSanPham.MaTinhTrang
    public int? MaTinhThanh { get; set; }         // TinhThanh.MaTinhThanh (khu vực sinh sống, không phải khoảng cách)
    public int? MaPhuongXa { get; set; }          // PhuongXa.MaPhuongXa
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? PriceRange { get; set; }          // index trong KhoPhanGia; ưu tiên hơn MinPrice/MaxPrice
    public string? Sort { get; set; }             // newest | price_asc | price_desc
    public int Page { get; set; } = 1;

    public const int PageSize = 24;

    public static readonly PriceRangeOption[] KhoPhanGia =
    [
        new(null, null, "Tất cả mức giá"),
        new(0, 100_000, "Dưới 100.000đ"),
        new(100_000, 350_000, "100.000đ - 350.000đ"),
        new(350_000, 1_000_000, "350.000đ - 1.000.000đ"),
        new(1_000_000, 5_000_000, "1.000.000đ - 5.000.000đ"),
        new(5_000_000, null, "Trên 5.000.000đ")
    ];

    /// <summary>Áp phân khúc giá đã chọn vào MinPrice/MaxPrice (giữ nguyên nếu tự nhập).</summary>
    public void GiaiPhanGia()
    {
        if (PriceRange is null or 0) return;
        if (PriceRange.Value < 0 || PriceRange.Value >= KhoPhanGia.Length) return;
        MinPrice = KhoPhanGia[PriceRange.Value].Min;
        MaxPrice = KhoPhanGia[PriceRange.Value].Max;
    }

    public string? TenPhanGia =>
        PriceRange > 0 && PriceRange < KhoPhanGia.Length ? KhoPhanGia[PriceRange.Value].Label : null;

    public static readonly (string Key, string Label)[] KhoSapXep =
    [
        ("newest", "Mới nhất"),
        ("price_asc", "Giá thấp đến cao"),
        ("price_desc", "Giá cao xuống thấp")
    ];
}

public sealed record PriceRangeOption(long? Min, long? Max, string Label);

/// <summary>Kết quả tìm kiếm kèm bộ lọc đang áp dụng để view giữ đúng trạng thái checkbox/radio.</summary>
public sealed class ProductSearchResultVm
{
    public required ProductSearchVm Filter { get; init; }
    public required PagedResult<ProductCardVm> Result { get; init; }

    public IReadOnlyList<DanhMuc> DanhSachDanhMuc { get; init; } = [];
    public IReadOnlyList<TinhTrangSanPham> DanhSachTinhTrang { get; init; } = [];

    /// <summary>Tên tỉnh/xã đang lọc, hiển thị trên chip bộ lọc; null khi chưa có hoặc chưa chọn.</summary>
    public string? KhuVucDaChon { get; init; }
}
