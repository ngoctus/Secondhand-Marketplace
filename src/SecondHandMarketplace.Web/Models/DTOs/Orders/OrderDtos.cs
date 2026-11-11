using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.DTOs.Orders;

public class CreateOrderRequest
{
    [Required(ErrorMessage = "Tên người nhận không được để trống.")]
    [StringLength(100)]
    public string ReceiverName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại nhận hàng không được để trống.")]
    [RegularExpression(@"^[0-9]{9,15}$", ErrorMessage = "Số điện thoại từ 9 đến 15 chữ số.")]
    public string ReceiverPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ nhận hàng không được để trống.")]
    [StringLength(500)]
    public string ReceiverAddress { get; set; } = string.Empty;

    /// <summary>
    /// Khung giờ nhận hàng: "NE_GIO_HANH_CHINH", "GIO_HANH_CHINH", "BAT_KY"
    /// </summary>
    [StringLength(100)]
    public string? DeliveryTimeSlot { get; set; } = "BAT_KY";

    /// <summary>
    /// Phương thức thanh toán: "COD", "BANK_TRANSFER"
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
    public string PaymentMethod { get; set; } = "COD";

    /// <summary>
    /// Hình thức nhận hàng: "GIAO_HANG", "GAP_TRUC_TIEP"
    /// </summary>
    [StringLength(20)]
    public string? DeliveryMethod { get; set; } = "GIAO_HANG";

    [StringLength(500)]
    public string? Note { get; set; }
}

public class BuyNowRequest : CreateOrderRequest
{
    [Required(ErrorMessage = "Mã sản phẩm mua ngay không được để trống.")]
    public int ProductId { get; set; }
}

public class UpdateOrderStatusRequest
{
    [Required(ErrorMessage = "Trạng thái mới không được để trống.")]
    public string NewStatus { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Note { get; set; }
}

public class CancelOrderRequest
{
    [Required(ErrorMessage = "Lý do hủy đơn không được để trống.")]
    [StringLength(500, ErrorMessage = "Lý do hủy không quá 500 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}

public class OrderItemResponse
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public decimal PriceAtPurchase { get; set; }
}

public class OrderStatusHistoryResponse
{
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; }
}

public class OrderResponse
{
    public int Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public int BuyerId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string DeliveryMethod { get; set; } = "GIAO_HANG";
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string ReceiverAddress { get; set; } = string.Empty;
    public string? DeliveryTimeSlot { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemsCount { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();
}

public class OrderDetailResponse : OrderResponse
{
    public List<OrderStatusHistoryResponse> StatusHistory { get; set; } = new();
}
