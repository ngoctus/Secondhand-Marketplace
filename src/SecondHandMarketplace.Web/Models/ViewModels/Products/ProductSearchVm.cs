namespace SecondHandMarketplace.Web.Models.ViewModels.Products;

/// <summary>Bộ lọc tìm kiếm sản phẩm (lấy từ query string: /Products?Keyword=iphone&amp;MaxPrice=5000000)</summary>
public class ProductSearchVm
{
    public string? Keyword { get; set; }          // tên sản phẩm hoặc tên cửa hàng
    public int? CategoryId { get; set; }          // DanhMuc.MaDanhMuc
    public int? ConditionId { get; set; }         // TinhTrangSanPham.MaTinhTrang
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}