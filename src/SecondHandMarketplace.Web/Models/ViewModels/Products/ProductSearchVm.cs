namespace SecondHandMarketplace.Web.Models.ViewModels.Products;

/// <summary>Bộ lọc tìm kiếm sản phẩm (lấy từ query string: /Products?Keyword=iphone&amp;MaxPrice=5000000&amp;Page=2)</summary>
public class ProductSearchVm
{
    public string? Keyword { get; set; }          // tên sản phẩm hoặc tên cửa hàng
    public int? CategoryId { get; set; }          // DanhMuc.MaDanhMuc (lọc 1 danh mục)
    public string[]? Categories { get; set; }     // nhiều danh mục từ checkbox sidebar (giá trị là Ten DanhMuc)
    public int? ConditionId { get; set; }         // TinhTrangSanPham.MaTinhTrang (lọc 1 tình trạng)
    public string? Condition { get; set; }        // tình trạng từ radio sidebar (new / like-new / good / fair / worn)
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Sort { get; set; }             // newest | price_asc | price_desc
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}
