using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.DTOs.Products;

public class ProductSearchRequest
{
    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? ConditionId { get; set; }
    public string? SortBy { get; set; } = "newest"; // newest, price_asc, price_desc
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class CreateProductRequest
{
    [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Tên sản phẩm từ 5 đến 200 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả sản phẩm không được để trống.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Mô tả phải từ 10 đến 2000 ký tự.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Giá bán không được để trống.")]
    [Range(1000, 10000000000, ErrorMessage = "Giá sản phẩm từ 1.000 đến 10.000.000.000 VNĐ.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn tình trạng sản phẩm.")]
    public int ConditionId { get; set; }

    public List<string>? ImageUrls { get; set; }
}

public class UpdateProductRequest
{
    [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Tên sản phẩm từ 5 đến 200 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả sản phẩm không được để trống.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Mô tả phải từ 10 đến 2000 ký tự.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Giá bán không được để trống.")]
    [Range(1000, 10000000000, ErrorMessage = "Giá sản phẩm từ 1.000 đến 10.000.000.000 VNĐ.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn tình trạng sản phẩm.")]
    public int ConditionId { get; set; }
}

public class ProductImageDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool IsMain { get; set; }
}

public class ProductResponse
{
    public int Id { get; set; }
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? ConditionId { get; set; }
    public string? ConditionName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ProductDetailResponse
{
    public int Id { get; set; }
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string? ShopLogo { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? ConditionId { get; set; }
    public string? ConditionName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
    public bool IsAuction { get; set; }
    public int? AuctionId { get; set; }
}
