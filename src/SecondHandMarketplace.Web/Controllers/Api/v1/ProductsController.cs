using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.DTOs.Products;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Services.Moderation;
using SecondHandMarketplace.Web.Services.Products;

namespace SecondHandMarketplace.Web.Controllers.Api.v1;

/// <summary>
/// Epic 2 & 4: Tìm kiếm, hiển thị chi tiết và quản lý sản phẩm.
/// </summary>
[ApiController]
[Route("api/v1/products")]
[Produces("application/json")]
public class ProductsController(
    IProductService productService,
    IModerationService moderationService,
    ILogger<ProductsController> logger) : ControllerBase
{
    /// <summary>
    /// Tìm kiếm và lọc danh sách sản phẩm công khai có phân trang.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceResult<PagedResult<ProductResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] ProductSearchRequest request)
    {
        var filter = new ProductSearchVm
        {
            Keyword = request.Keyword,
            CategoryId = request.CategoryId,
            ConditionId = request.ConditionId,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            Sort = request.SortBy ?? "newest",
            Page = request.Page < 1 ? 1 : request.Page,
            PageSize = request.PageSize < 1 ? 12 : request.PageSize
        };

        var pagedResult = await productService.SearchAsync(filter);

        var productResponses = pagedResult.Items.Select(p => new ProductResponse
        {
            Id = p.Id,
            ShopId = 0,
            ShopName = p.ShopName,
            CategoryId = null,
            CategoryName = p.CategoryName,
            ConditionId = null,
            ConditionName = p.ConditionName,
            Name = p.Name,
            Price = p.Price,
            ThumbnailUrl = p.ImageUrl,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        }).ToList();

        var result = new PagedResult<ProductResponse>(
            productResponses, pagedResult.TotalItems, pagedResult.Page, pagedResult.PageSize);

        return ServiceResult<PagedResult<ProductResponse>>.Ok(result).ToActionResult(this);
    }

    /// <summary>
    /// Xem chi tiết sản phẩm.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceResult<ProductDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(int id)
    {
        var product = await productService.GetDetailAsync(id);
        if (product is null)
        {
            return ServiceResult<ProductDetailResponse>.Fail(ErrorCodes.ProductNotFound, "Không tìm thấy sản phẩm.").ToActionResult(this);
        }

        var response = new ProductDetailResponse
        {
            Id = product.Id,
            ShopId = 0,
            ShopName = product.ShopName,
            ShopLogo = null,
            CategoryId = null,
            CategoryName = product.CategoryName,
            ConditionId = null,
            ConditionName = product.ConditionName,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Status = product.Status,
            CreatedAt = product.CreatedAt,
            IsAuction = false,
            AuctionId = null,
            Images = product.Images.Select(img => new ProductImageDto
            {
                Id = 0,
                Url = img,
                IsMain = false
            }).ToList()
        };

        return ServiceResult<ProductDetailResponse>.Ok(response).ToActionResult(this);
    }

    /// <summary>
    /// Danh sách sản phẩm của người bán (Shop mình).
    /// </summary>
    [HttpGet("my-products")]
    [Authorize]
    [ProducesResponseType(typeof(ServiceResult<PagedResult<MyProductVm>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProducts([FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult<PagedResult<MyProductVm>>.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var list = await productService.GetMyAsync(userId, page, cancellationToken);
        return ServiceResult<PagedResult<MyProductVm>>.Ok(list).ToActionResult(this);
    }

    /// <summary>
    /// Tạo sản phẩm mới (trạng thái DRAFT).
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ServiceResult<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ServiceResult<int>.Fail(ErrorCodes.ValidationError, "Dữ liệu sản phẩm không hợp lệ.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()).ToActionResult(this);
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult<int>.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var form = new ProductFormVm
        {
            Ten = request.Name,
            MoTa = request.Description,
            Gia = request.Price,
            MaDanhMuc = request.CategoryId,
            MaTinhTrang = request.ConditionId
        };

        var images = (request.ImageUrls ?? new List<string>())
            .Select(url => new ProductImageInput(url, 0))
            .ToList();

        var (success, error, productId) = await productService.CreateAsync(userId, form, images, cancellationToken);
        if (!success)
        {
            return ServiceResult<int>.Fail(ErrorCodes.BadRequest, error ?? "Không thể tạo sản phẩm.").ToActionResult(this);
        }

        return ServiceResult<int>.Ok(productId, "Tạo sản phẩm thành công.").ToActionResult(this);
    }

    /// <summary>
    /// Cập nhật thông tin sản phẩm.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ServiceResult.Fail(ErrorCodes.ValidationError, "Dữ liệu cập nhật không hợp lệ.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()).ToActionResult(this);
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var form = new ProductFormVm
        {
            Id = id,
            Ten = request.Name,
            MoTa = request.Description,
            Gia = request.Price,
            MaDanhMuc = request.CategoryId,
            MaTinhTrang = request.ConditionId
        };

        var (success, error) = await productService.UpdateAsync(userId, id, form, Array.Empty<ProductImageInput>(), cancellationToken);
        if (!success)
        {
            return ServiceResult.Fail(ErrorCodes.BadRequest, error ?? "Không thể cập nhật sản phẩm.").ToActionResult(this);
        }

        return ServiceResult.Ok("Cập nhật sản phẩm thành công.").ToActionResult(this);
    }

    /// <summary>
    /// Gửi duyệt sản phẩm (chuyển sang PENDING_REVIEW).
    /// </summary>
    [HttpPost("{id:int}/submit-review")]
    [Authorize]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitReview(int id, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var (success, error) = await productService.GuiDuyetAsync(userId, id, cancellationToken);
        if (!success)
        {
            return ServiceResult.Fail(ErrorCodes.ModerationRuleViolation, error ?? "Không thể gửi duyệt sản phẩm.").ToActionResult(this);
        }

        // C2: quét từ khóa ngay sau khi sp_GuiDuyetSanPham thành công (lượt + PhanTichAI QUEUED đã được tạo).
        // Lỗi quét bất ngờ không được làm hỏng kết quả gửi duyệt: sản phẩm đã ở PENDING_REVIEW và Admin có thể quét lại.
        try
        {
            await moderationService.PhanTichSanPhamAsync(id, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Phân tích từ khóa lỗi sau khi gửi duyệt sản phẩm {ProductId}.", id);
        }

        return ServiceResult.Ok("Sản phẩm đã được gửi duyệt thành công.").ToActionResult(this);
    }

    /// <summary>
    /// Ẩn sản phẩm đang bán.
    /// </summary>
    [HttpPost("{id:int}/hide")]
    [Authorize]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Hide(int id, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var (success, error) = await productService.HideAsync(userId, id, cancellationToken);
        if (!success)
        {
            return ServiceResult.Fail(ErrorCodes.BadRequest, error ?? "Không thể ẩn sản phẩm.").ToActionResult(this);
        }

        return ServiceResult.Ok("Đã ẩn sản phẩm.").ToActionResult(this);
    }

    /// <summary>
    /// Xóa mềm sản phẩm.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var (success, error) = await productService.DeleteAsync(userId, id, cancellationToken);
        if (!success)
        {
            return ServiceResult.Fail(ErrorCodes.BadRequest, error ?? "Không thể xóa sản phẩm.").ToActionResult(this);
        }

        return ServiceResult.Ok("Xóa sản phẩm thành công.").ToActionResult(this);
    }
}
