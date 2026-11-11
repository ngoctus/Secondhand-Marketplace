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

        var model = new CheckoutViewModel
        {
            TenNguoiNhan = request.ReceiverName,
            SoDienThoaiNhan = request.ReceiverPhone,
            DiaChiNhan = request.ReceiverAddress,
            PhuongThucThanhToan = request.PaymentMethod,
            HinhThucNhan = request.DeliveryMethod ?? "GIAO_HANG"
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

        var model = new CheckoutViewModel
        {
            TenNguoiNhan = request.ReceiverName,
            SoDienThoaiNhan = request.ReceiverPhone,
            DiaChiNhan = request.ReceiverAddress,
            PhuongThucThanhToan = request.PaymentMethod,
            MaSanPhamMuaNgay = request.ProductId,
            HinhThucNhan = request.DeliveryMethod ?? "GIAO_HANG"
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

        var orders = await orderService.LayDanhSachDonMuaAsync(userId, status);
        var dtos = orders.Select(o => new OrderResponse
        {
            Id = o.MaDonHang,
            OrderCode = o.MaDon,
            ShopId = o.MaCuaHang,
            ShopName = o.TenShop,
            Status = o.TrangThai,
            TotalAmount = o.TongTien,
            PaymentMethod = o.PhuongThucThanhToan,
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

        var order = await orderService.LayChiTietDonMuaAsync(userId, orderId);
        if (order is null)
        {
            return ServiceResult<OrderDetailResponse>.Fail(ErrorCodes.OrderNotFound, "Không tìm thấy đơn hàng.").ToActionResult(this);
        }

        var detail = new OrderDetailResponse
        {
            Id = order.MaDonHang,
            OrderCode = order.MaDon,
            ShopId = order.MaCuaHang,
            ShopName = order.TenShop,
            Status = order.TrangThai,
            TotalAmount = order.TongTien,
            ShippingFee = order.PhiVanChuyen,
            PaymentMethod = order.PhuongThucThanhToan,
            ReceiverName = order.TenNguoiNhan,
            ReceiverPhone = order.SoDienThoaiNhan,
            ReceiverAddress = order.DiaChiNhan,
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
