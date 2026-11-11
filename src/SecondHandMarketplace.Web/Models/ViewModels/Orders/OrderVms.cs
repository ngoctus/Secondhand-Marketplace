namespace SecondHandMarketplace.Web.Models.ViewModels.Orders;

public sealed class OrderRowVm
{
    public int Id { get; set; }
    public string MaDon { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public string? FirstItem { get; set; }
    public string? FirstImage { get; set; }
    public int BuyerId { get; set; }
    public string BuyerName { get; set; } = "";
    public string ShopName { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
}

public sealed class OrderItemVm
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public string? Image { get; set; }
    public decimal Price { get; set; }
}

public sealed class OrderHistoryVm
{
    public string? From { get; set; }
    public string To { get; set; } = "";
    public string Actor { get; set; } = "";
    public string? Reason { get; set; }
    public DateTime At { get; set; }
}

public sealed class OrderPaymentVm
{
    public string Method { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal Amount { get; set; }
    public string? TransactionCode { get; set; }
    public DateTime? PaidAt { get; set; }
}

public sealed class OrderDetailVm
{
    public int Id { get; set; }
    public string MaDon { get; set; } = "";
    public string Status { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public decimal ItemsTotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Total { get; set; }
    public string RecipientName { get; set; } = "";
    public string RecipientPhone { get; set; } = "";
    public string RecipientAddress { get; set; } = "";
    public string? DeliveryWindow { get; set; }
    public int BuyerId { get; set; }
    public string BuyerName { get; set; } = "";
    public string ShopName { get; set; } = "";
    public string SellerName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? CancelledBy { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public List<OrderItemVm> Items { get; set; } = new();
    public List<OrderHistoryVm> History { get; set; } = new();
    public List<OrderPaymentVm> Payments { get; set; } = new();
}

public sealed record ShopOptionVm(int Id, string Name);

public sealed class OrderListVm
{
    public PagedResult<OrderRowVm> Result { get; set; } = new();
    public string? Status { get; set; }
    public string? Q { get; set; }
    public Dictionary<string, int> Counts { get; set; } = new();
}

public sealed class AdminOrderFilter
{
    public string? Status { get; set; }
    public string? Q { get; set; }
    public int? ShopId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class AdminOrderListVm
{
    public PagedResult<OrderRowVm> Result { get; set; } = new();
    public AdminOrderFilter Filter { get; set; } = new();
    public List<ShopOptionVm> Shops { get; set; } = new();
}
