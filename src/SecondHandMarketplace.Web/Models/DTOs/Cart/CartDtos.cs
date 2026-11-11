using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.DTOs.Cart;

public class AddToCartRequest
{
    [Required(ErrorMessage = "Mã sản phẩm không được để trống.")]
    public int ProductId { get; set; }
}

public class UpdateCartItemRequest
{
    public bool IsSelected { get; set; }
}

public class CartItemResponse
{
    public int CartItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public bool IsAvailable { get; set; }
}

public class CartResponse
{
    public List<CartItemResponse> Items { get; set; } = new();
    public int TotalItems => Items.Count;
    public int SelectedItemsCount => Items.Count(x => x.IsSelected && x.IsAvailable);
    public decimal SelectedTotalAmount => Items.Where(x => x.IsSelected && x.IsAvailable).Sum(x => x.Price);
}
