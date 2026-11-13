namespace SecondHandMarketplace.Web.Models.ViewModels.Products;

/// <summary>Dữ liệu hiển thị 1 sản phẩm trong danh sách.</summary>
public class ProductCardVm
{
    /// <summary>Số ký tự đầu của mô tả được hiển thị trên thẻ (kế hoạch B1).</summary>
    public const int DoDaiMoTaNgan = 100;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string? ShopLogoUrl { get; set; }
    public string? ConditionName { get; set; }
    public string? CategoryName { get; set; }
    public string? MoTaNgan { get; set; }

    /// <summary>ACTIVE | RESERVED | SOLD (xem PUBLIC, RESERVED, SOLD).</summary>
    public string Status { get; set; } = "ACTIVE";

    /// <summary>Điểm trung bình từ vw_DiemCuaHang; null khi chưa có dữ liệu đánh giá.</summary>
    public decimal? DiemTrungBinh { get; set; }
    public int SoDanhGia { get; set; }

    /// <summary>"Phường/Xã, Tỉnh/Thành"; null khi shop chưa khai địa chỉ.</summary>
    public string? KhuVuc { get; set; }

    public bool ConHang => Status == "ACTIVE";

    public string TrangThaiHienThi => Status switch
    {
        "RESERVED" => "Đã giữ",
        "SOLD" => "Đã bán",
        _ => "Còn hàng"
    };

    public bool CoDanhGia => DiemTrungBinh.HasValue && SoDanhGia > 0;

    public string DanhGiaHienThi => CoDanhGia ? $"{DiemTrungBinh:0.0} · {SoDanhGia} đánh giá" : "Chưa có đánh giá";
    public string KhuVucHienThi => string.IsNullOrWhiteSpace(KhuVuc) ? "Chưa cập nhật khu vực" : KhuVuc;

    /// <summary>Cắt ~100 ký tự đầu, dùng chung cho mọi nơi dựng thẻ (kể cả giỏ hàng, yêu thích).</summary>
    public static string? TaoMoTaNgan(string? moTa)
    {
        moTa = moTa?.Trim();
        if (string.IsNullOrEmpty(moTa)) return null;
        return moTa.Length <= DoDaiMoTaNgan ? moTa : moTa[..DoDaiMoTaNgan].TrimEnd() + "…";
    }
}
