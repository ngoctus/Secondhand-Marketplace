namespace SecondHandMarketplace.Web.Models.ViewModels.Products;

/// <summary>Dữ liệu trang chi tiết sản phẩm.</summary>
public class ProductDetailVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;   // ACTIVE | RESERVED | SOLD
    public bool CanBuy { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? ConditionName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Images { get; set; } = new();

    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string? ShopDescription { get; set; }
    public string? ShopLogoUrl { get; set; }

    /// <summary>Từ vw_DiemCuaHang (C); null khi chưa có dữ liệu đánh giá.</summary>
    public decimal? DiemTrungBinh { get; set; }
    public int SoDanhGia { get; set; }
    public int SoDonThanhCong { get; set; }

    /// <summary>"Phường/Xã, Tỉnh/Thành" từ dữ liệu khu vực của A.</summary>
    public string? KhuVuc { get; set; }

    public bool CoDanhGia => DiemTrungBinh.HasValue && SoDanhGia > 0;

    public string TrangThaiHienThi => Status switch
    {
        "RESERVED" => "Đang được giữ chỗ",
        "SOLD" => "Đã bán",
        _ => "Đang bán"
    };
}
