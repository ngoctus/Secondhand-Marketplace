using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.DTOs.Cart;
using SecondHandMarketplace.Web.Services.Carts;

namespace SecondHandMarketplace.Web.Controllers.Api.v1;

/// <summary>
/// Epic 5: Quản lý giỏ hàng của người mua.
/// </summary>
[ApiController]
[Route("api/v1/cart")]
[Authorize]
[Produces("application/json")]
public class CartController(ICartService cartService) : ControllerBase
{
    /// <summary>
    /// Lấy toàn bộ sản phẩm trong giỏ hàng hiện tại.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ServiceResult<CartResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult<CartResponse>.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var cartVm = await cartService.LayGioHangAsync(userId);

        var items = cartVm.DanhSachNhomShop.SelectMany(group => group.Items.Select(item => new CartItemResponse
        {
            CartItemId = item.MaGioHang,
            ProductId = item.MaSanPham,
            ProductName = item.TenSanPham,
            Price = item.Gia,
            ThumbnailUrl = item.AnhDaiDien,
            ShopId = group.MaCuaHang,
            ShopName = group.TenShop,
            IsSelected = item.DaChon,
            IsAvailable = item.CoTheMua
        })).ToList();

        var response = new CartResponse { Items = items };
        return ServiceResult<CartResponse>.Ok(response).ToActionResult(this);
    }

    /// <summary>
    /// Thêm sản phẩm vào giỏ hàng.
    /// </summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var (success, message) = await cartService.ThemVaoGioAsync(userId, request.ProductId);
        if (!success)
        {
            return ServiceResult.Fail(ErrorCodes.BadRequest, message ?? "Không thể thêm sản phẩm vào giỏ hàng.").ToActionResult(this);
        }

        return ServiceResult.Ok(message ?? "Đã thêm sản phẩm vào giỏ hàng.").ToActionResult(this);
    }

    /// <summary>
    /// Bật/tắt chọn một món hàng để đặt hàng.
    /// </summary>
    [HttpPut("items/{cartItemId:int}/selection")]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateItemSelection(int cartItemId, [FromBody] UpdateCartItemRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var success = await cartService.CapNhatTrangThaiChonAsync(userId, cartItemId, request.IsSelected);
        if (!success)
        {
            return ServiceResult.Fail(ErrorCodes.CartItemNotFound, "Không tìm thấy món hàng trong giỏ.").ToActionResult(this);
        }

        return ServiceResult.Ok("Đã cập nhật trạng thái chọn.").ToActionResult(this);
    }

    /// <summary>
    /// Xóa một món hàng khỏi giỏ hàng.
    /// </summary>
    [HttpDelete("items/{cartItemId:int}")]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveFromCart(int cartItemId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var success = await cartService.XoaKhoiGioAsync(userId, cartItemId);
        if (!success)
        {
            return ServiceResult.Fail(ErrorCodes.CartItemNotFound, "Không tìm thấy món hàng để xóa.").ToActionResult(this);
        }

        return ServiceResult.Ok("Đã xóa món hàng khỏi giỏ.").ToActionResult(this);
    }
}
