using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Areas;
using SecondHandMarketplace.Web.Models.ViewModels;

namespace SecondHandMarketplace.Web.Models.ViewModels.Products;

/// <summary>Bộ lọc tìm kiếm sản phẩm (query string: /Products?Keyword=iphone&amp;PriceRange=2&amp;sort=price_asc&amp;page=2)</summary>
public class ProductSearchVm
{
    [System.ComponentModel.DataAnnotations.StringLength(200, ErrorMessage = "Từ khóa tìm kiếm không vượt quá 200 ký tự.")]
    public string? Keyword { get; set; }          // tên sản phẩm, tên cửa hàng hoặc tên danh mục
    public int? CategoryId { get; set; }          // DanhMuc.MaDanhMuc
    public int? ConditionId { get; set; }         // TinhTrangSanPham.MaTinhTrang
    public int? MaTinhThanh { get; set; }         // TinhThanh.MaTinhThanh (khu vực sinh sống, không phải khoảng cách)
    public int? MaPhuongXa { get; set; }          // PhuongXa.MaPhuongXa
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? PriceRange { get; set; }          // index trong KhoPhanGia; ưu tiên hơn MinPrice/MaxPrice
    public string? Sort { get; set; }             // newest | price_asc | price_desc
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>Mức phân trang dùng khi caller (API, AI tool, trang chủ) không tự chỉ định (3 hàng x 3 cột = 9 sản phẩm/trang).</summary>
    public const int DefaultPageSize = 9;

    /// <summary>
    /// Phân khúc giá đọc theo ngôn ngữ hiện tại, nên phải là property: mảng static sẽ đóng băng
    /// nhãn của văn hóa đầu tiên chạm tới và dính sang mọi request sau đó.
    /// </summary>
    public static PriceRangeOption[] KhoPhanGia =>
    [
        new(null, null, HienThi.Chon("Tất cả mức giá", "All prices")),
        new(0, 100_000, HienThi.Chon("Dưới 100.000đ", "Under 100.000đ")),
        new(100_000, 350_000, HienThi.Chon("100.000đ - 350.000đ", "100.000đ - 350.000đ")),
        new(350_000, 1_000_000, HienThi.Chon("350.000đ - 1.000.000đ", "350.000đ - 1.000.000đ")),
        new(1_000_000, 5_000_000, HienThi.Chon("1.000.000đ - 5.000.000đ", "1.000.000đ - 5.000.000đ")),
        new(5_000_000, null, HienThi.Chon("Trên 5.000.000đ", "Over 5.000.000đ"))
    ];

    /// <summary>Áp phân khúc giá đã chọn vào MinPrice/MaxPrice; bỏ nếu người mua tự kéo slider (B-04).</summary>
    public void GiaiPhanGia()
    {
        if (PriceRange is null or 0) return;
        if (PriceRange.Value < 0 || PriceRange.Value >= KhoPhanGia.Length) return;
        if (MinPrice != null || MaxPrice != null) return;
        MinPrice = KhoPhanGia[PriceRange.Value].Min;
        MaxPrice = KhoPhanGia[PriceRange.Value].Max;
    }

    public string? TenPhanGia =>
        PriceRange > 0 && PriceRange < KhoPhanGia.Length ? KhoPhanGia[PriceRange.Value].Label : null;

    public static (string Key, string Label)[] KhoSapXep =>
    [
        ("newest", HienThi.Chon("Mới nhất", "Newest first")),
        ("price_asc", HienThi.Chon("Giá thấp đến cao", "Price: low to high")),
        ("price_desc", HienThi.Chon("Giá cao xuống thấp", "Price: high to low"))
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

    /// <summary>Dữ liệu dropdown khu vực của thành phần _ChonKhuVuc (mục A4).</summary>
    public ChonKhuVucVm KhuVuc { get; init; } = new();

    /// <summary>Tên tỉnh/xã đang lọc, hiển thị trên chip bộ lọc; null khi chưa có hoặc chưa chọn.</summary>
    public string? KhuVucDaChon { get; init; }

    /// <summary>Hai đầu của thanh trượt giá (B-04) lấy từ giá thật thấp/cao nhất đang bán; null khi chưa có tin.</summary>
    public decimal? BienGiaMin { get; init; }
    public decimal? BienGiaMax { get; init; }
}
