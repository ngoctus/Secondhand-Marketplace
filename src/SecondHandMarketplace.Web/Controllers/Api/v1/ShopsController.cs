using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.DTOs.Shops;
using SecondHandMarketplace.Web.Services.Shops;

namespace SecondHandMarketplace.Web.Controllers.Api.v1;

/// <summary>
/// Epic 2: Quản lý thông tin cửa hàng (Shop người bán).
/// </summary>
[ApiController]
[Route("api/v1/shops")]
[Produces("application/json")]
public class ShopsController(IShopService shopService) : ControllerBase
{
    /// <summary>
    /// Lấy thông tin shop của người dùng hiện tại (nếu có).
    /// </summary>
    [HttpGet("my-shop")]
    [Authorize]
    [ProducesResponseType(typeof(ServiceResult<ShopResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyShop(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult<ShopResponse>.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var shop = await shopService.GetByOwnerAsync(userId, cancellationToken);
        if (shop is null)
        {
            return ServiceResult<ShopResponse>.Fail(ErrorCodes.ShopNotFound, "Tài khoản chưa đăng ký mở Shop.").ToActionResult(this);
        }

        var response = new ShopResponse
        {
            Id = shop.MaCuaHang,
            OwnerId = shop.MaNguoiDung,
            OwnerName = shop.NguoiDung?.HoTen ?? string.Empty,
            Name = shop.TenShop,
            Description = shop.MoTa,
            Logo = shop.Logo,
            ContactPhone = shop.SoDienThoaiLienHe,
            Status = shop.TrangThai,
            CreatedAt = shop.NgayTao
        };

        return ServiceResult<ShopResponse>.Ok(response).ToActionResult(this);
    }

    /// <summary>
    /// Tạo mới hoặc cập nhật thông tin shop của người bán.
    /// </summary>
    [HttpPost("my-shop")]
    [Authorize]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrUpdateShop([FromBody] CreateShopRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ServiceResult.Fail(ErrorCodes.ValidationError, "Dữ liệu shop không hợp lệ.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()).ToActionResult(this);
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var (success, error) = await shopService.SaveAsync(userId, new ShopSaveInput(
            request.Name, request.ContactPhone, request.Description, request.Logo,
            request.ProvinceId, request.WardId, request.AddressDetail, request.AreaLabel,
            request.BankBin, request.BankAccountNumber, request.BankAccountHolder), cancellationToken);

        if (!success)
        {
            return ServiceResult.Fail(ErrorCodes.BadRequest, error ?? "Không thể lưu thông tin shop.").ToActionResult(this);
        }

        return ServiceResult.Ok("Lưu thông tin shop thành công.").ToActionResult(this);
    }
}
