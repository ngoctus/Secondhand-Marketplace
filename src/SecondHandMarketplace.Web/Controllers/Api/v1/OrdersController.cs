using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.DTOs.Orders;
using SecondHandMarketplace.Web.Models.ViewModels.Orders;
using SecondHandMarketplace.Web.Services.OrderManagement;
using SecondHandMarketplace.Web.Services.Orders;

namespace SecondHandMarketplace.Web.Controllers.Api.v1;

/// <summary>
/// Epic 5 & 6: Quản lý đặt hàng, thanh toán và vòng đời đơn hàng.
/// </summary>
[ApiController]
[Route("api/v1/orders")]
[Authorize]
[Produces("application/json")]
public class OrdersController(
    IOrderService orderService,
    IOrderManagementService orderManagementService) : ControllerBase
{
    /// <summary>
    /// Đặt hàng từ các sản phẩm đã chọn trong giỏ hàng.
    /// </summary>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Checkout([FromBody] CreateOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ServiceResult.Fail(ErrorCodes.ValidationError, "Thông tin đặt hàng không hợp lệ.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()).ToActionResult(this);
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var validPaymentMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "COD", "BANK_TRANSFER", "ONLINE" };
        var validDeliveryMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GIAO_HANG", "GAP_TRUC_TIEP" };

        if (string.IsNullOrWhiteSpace(request.PaymentMethod) || !validPaymentMethods.Contains(request.PaymentMethod))
        {
            return ServiceResult.Fail(ErrorCodes.ValidationError, "Phương thức thanh toán không hợp lệ. Chỉ chấp nhận COD, BANK_TRANSFER hoặc ONLINE.").ToActionResult(this);
        }

        if (!string.IsNullOrWhiteSpace(request.DeliveryMethod) && !validDeliveryMethods.Contains(request.DeliveryMethod))
        {
            return ServiceResult.Fail(ErrorCodes.ValidationError, "Hình thức nhận hàng không hợp lệ. Chỉ chấp nhận GIAO_HANG hoặc GAP_TRUC_TIEP.").ToActionResult(this);
        }

        var diaChi = request.ReceiverAddress.Trim();
        var extraParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.DeliveryTimeSlot) && request.DeliveryTimeSlot != "BAT_KY")
        {
            extraParts.Add($"Khung giờ: {request.DeliveryTimeSlot.Trim()}");
        }
        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            extraParts.Add($"Ghi chú: {request.Note.Trim()}");
        }
        if (extraParts.Count > 0)
        {
            var extraStr = $" ({string.Join(" - ", extraParts)})";
            if (diaChi.Length + extraStr.Length <= 500)
            {
                diaChi += extraStr;
            }
        }

        var model = new CheckoutViewModel
        {
            TenNguoiNhan = request.ReceiverName,
            SoDienThoaiNhan = request.ReceiverPhone,
            DiaChiNhan = diaChi,
            PhuongThucThanhToan = request.PaymentMethod.ToUpperInvariant(),
            HinhThucNhan = (request.DeliveryMethod ?? "GIAO_HANG").ToUpperInvariant()
        };

        var result = await orderService.DatHangTuGioAsync(userId, model);
        if (!result.ThanhCong)
        {
            return ServiceResult.Fail(ErrorCodes.BadRequest, result.ThongBao ?? "Đặt hàng thất bại.").ToActionResult(this);
        }

        return ServiceResult<object>.Ok(new
        {
            OrderCodes = result.DanhSachMaDon,
            Message = result.ThongBao
        }, "Đặt hàng thành công!").ToActionResult(this);
    }

    /// <summary>
    /// Mua ngay một sản phẩm đơn lẻ (bỏ qua giỏ hàng).
    /// </summary>
    [HttpPost("buy-now")]
    [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BuyNow([FromBody] BuyNowRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ServiceResult.Fail(ErrorCodes.ValidationError, "Thông tin mua ngay không hợp lệ.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()).ToActionResult(this);
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var validPaymentMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "COD", "BANK_TRANSFER", "ONLINE" };
        var validDeliveryMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GIAO_HANG", "GAP_TRUC_TIEP" };

        if (string.IsNullOrWhiteSpace(request.PaymentMethod) || !validPaymentMethods.Contains(request.PaymentMethod))
        {
            return ServiceResult.Fail(ErrorCodes.ValidationError, "Phương thức thanh toán không hợp lệ. Chỉ chấp nhận COD, BANK_TRANSFER hoặc ONLINE.").ToActionResult(this);
        }

        if (!string.IsNullOrWhiteSpace(request.DeliveryMethod) && !validDeliveryMethods.Contains(request.DeliveryMethod))
        {
            return ServiceResult.Fail(ErrorCodes.ValidationError, "Hình thức nhận hàng không hợp lệ. Chỉ chấp nhận GIAO_HANG hoặc GAP_TRUC_TIEP.").ToActionResult(this);
        }

        var diaChiBuyNow = request.ReceiverAddress.Trim();
        var extraPartsBuyNow = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.DeliveryTimeSlot) && request.DeliveryTimeSlot != "BAT_KY")
        {
            extraPartsBuyNow.Add($"Khung giờ: {request.DeliveryTimeSlot.Trim()}");
        }
        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            extraPartsBuyNow.Add($"Ghi chú: {request.Note.Trim()}");
        }
        if (extraPartsBuyNow.Count > 0)
        {
            var extraStr = $" ({string.Join(" - ", extraPartsBuyNow)})";
            if (diaChiBuyNow.Length + extraStr.Length <= 500)
            {
                diaChiBuyNow += extraStr;
            }
        }

        var model = new CheckoutViewModel
        {
            TenNguoiNhan = request.ReceiverName,
            SoDienThoaiNhan = request.ReceiverPhone,
            DiaChiNhan = diaChiBuyNow,
            PhuongThucThanhToan = request.PaymentMethod.ToUpperInvariant(),
            MaSanPhamMuaNgay = request.ProductId,
            HinhThucNhan = (request.DeliveryMethod ?? "GIAO_HANG").ToUpperInvariant()
        };

        var result = await orderService.MuaNgayAsync(userId, request.ProductId, model);
        if (!result.ThanhCong)
        {
            return ServiceResult.Fail(ErrorCodes.BadRequest, result.ThongBao ?? "Mua ngay thất bại.").ToActionResult(this);
        }

        return ServiceResult<object>.Ok(new
        {
            OrderCodes = result.DanhSachMaDon,
            Message = result.ThongBao
        }, "Đặt hàng thành công!").ToActionResult(this);
    }

    /// <summary>
    /// Danh sách đơn hàng đã mua của người dùng hiện tại.
    /// </summary>
    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(ServiceResult<IReadOnlyList<OrderResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders([FromQuery] string? status = null)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult<IReadOnlyList<OrderResponse>>.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var userName = User.Identity?.Name;
        var orders = await orderService.LayDanhSachDonMuaAsync(userId, status);
        var dtos = orders.Select(o => new OrderResponse
        {
            Id = o.MaDonHang,
            OrderCode = o.MaDon,
            BuyerId = userId,
            BuyerName = !string.IsNullOrEmpty(userName) ? userName : o.TenNguoiNhan,
            ShopId = o.MaCuaHang,
            ShopName = o.TenShop,
            Status = o.TrangThai,
            TotalAmount = o.TongTien,
            ShippingFee = o.PhiVanChuyen,
            PaymentMethod = o.PhuongThucThanhToan,
            DeliveryMethod = o.HinhThucNhan,
            ReceiverName = o.TenNguoiNhan,
            ReceiverPhone = o.SoDienThoaiNhan,
            ReceiverAddress = o.DiaChiNhan,
            CreatedAt = o.NgayTao,
            ItemsCount = o.ChiTiet.Count,
            Items = o.ChiTiet.Select(c => new OrderItemResponse
            {
                ProductId = c.MaSanPham,
                ProductName = c.TenSanPham,
                ThumbnailUrl = c.AnhDaiDien,
                PriceAtPurchase = c.Gia
            }).ToList()
        }).ToList();

        return ServiceResult<IReadOnlyList<OrderResponse>>.Ok(dtos).ToActionResult(this);
    }

    /// <summary>
    /// Xem chi tiết một đơn mua của người dùng.
    /// </summary>
    [HttpGet("{orderId:int}")]
    [ProducesResponseType(typeof(ServiceResult<OrderDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderDetail(int orderId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult<OrderDetailResponse>.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var userName = User.Identity?.Name;
        var order = await orderService.LayChiTietDonMuaAsync(userId, orderId);
        if (order is null)
        {
            return ServiceResult<OrderDetailResponse>.Fail(ErrorCodes.OrderNotFound, "Không tìm thấy đơn hàng.").ToActionResult(this);
        }

        string? deliveryTimeSlot = null;
        string? note = null;
        if (!string.IsNullOrEmpty(order.DiaChiNhan))
        {
            var mSlot = System.Text.RegularExpressions.Regex.Match(order.DiaChiNhan, @"Khung giờ:\s*([^-\)]+)");
            if (mSlot.Success) deliveryTimeSlot = mSlot.Groups[1].Value.Trim();

            var mNote = System.Text.RegularExpressions.Regex.Match(order.DiaChiNhan, @"Ghi chú:\s*([^\)]+)");
            if (mNote.Success) note = mNote.Groups[1].Value.Trim();
        }

        var detail = new OrderDetailResponse
        {
            Id = order.MaDonHang,
            OrderCode = order.MaDon,
            BuyerId = userId,
            BuyerName = !string.IsNullOrEmpty(userName) ? userName : order.TenNguoiNhan,
            ShopId = order.MaCuaHang,
            ShopName = order.TenShop,
            Status = order.TrangThai,
            TotalAmount = order.TongTien,
            ShippingFee = order.PhiVanChuyen,
            PaymentMethod = order.PhuongThucThanhToan,
            DeliveryMethod = order.HinhThucNhan,
            ReceiverName = order.TenNguoiNhan,
            ReceiverPhone = order.SoDienThoaiNhan,
            ReceiverAddress = order.DiaChiNhan,
            DeliveryTimeSlot = deliveryTimeSlot,
            Note = note,
            CreatedAt = order.NgayTao,
            ItemsCount = order.DanhSachSanPham.Count,
            Items = order.DanhSachSanPham.Select(p => new OrderItemResponse
            {
                ProductId = p.MaSanPham,
                ProductName = p.TenSanPham,
                ThumbnailUrl = p.AnhDaiDien,
                PriceAtPurchase = p.Gia
            }).ToList(),
            StatusHistory = order.LichSuTrangThai.Select(h => new OrderStatusHistoryResponse
            {
                Status = h.TrangThaiMoi,
                Note = h.LyDo,
                ChangedAt = h.ThoiGian
            }).ToList()
        };

        return ServiceResult<OrderDetailResponse>.Ok(detail).ToActionResult(this);
    }

    /// <summary>
    /// Người mua hủy đơn hàng khi đơn chưa được người bán xác nhận.
    /// </summary>
    [HttpPost("{orderId:int}/cancel")]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelOrder(int orderId, [FromBody] CancelOrderRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var (success, error) = await orderService.HuyDonMuaAsync(userId, orderId, request.Reason);
        if (!success)
        {
            return ServiceResult.Fail(ErrorCodes.OrderCannotCancel, error ?? "Không thể hủy đơn hàng này.").ToActionResult(this);
        }

        return ServiceResult.Ok("Hủy đơn hàng thành công.").ToActionResult(this);
    }

    /// <summary>
    /// Người bán cập nhật trạng thái đơn hàng (CONFIRMED, SHIPPING, DELIVERED).
    /// </summary>
    [HttpPost("{orderId:int}/seller-status")]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SellerUpdateStatus(
        int orderId,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var (ok, error) = await orderManagementService.SellerUpdateStatusAsync(
            userId, orderId, request.NewStatus, request.Note, cancellationToken);

        if (!ok)
        {
            return ServiceResult.Fail(ErrorCodes.OrderCannotUpdateStatus, error ?? "Không thể cập nhật trạng thái đơn.").ToActionResult(this);
        }

        return ServiceResult.Ok("Cập nhật trạng thái đơn hàng thành công.").ToActionResult(this);
    }
}
