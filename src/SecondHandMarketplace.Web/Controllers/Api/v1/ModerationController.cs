using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.DTOs.Moderation;
using SecondHandMarketplace.Web.Services.ProductReview;

namespace SecondHandMarketplace.Web.Controllers.Api.v1;

/// <summary>
/// Epic 3: Kiểm duyệt sản phẩm (Dành cho Quản trị viên).
/// </summary>
[ApiController]
[Route("api/v1/moderation")]
[Authorize(Roles = AppRoles.Admin)]
[Produces("application/json")]
public class ModerationController(IProductReviewService reviewService) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách sản phẩm chờ kiểm duyệt (PENDING_REVIEW).
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(ServiceResult<PagedResult<PendingProductResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingList([FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        var listPage = await reviewService.GetListAsync(ProductReviewTabs.Pending, page, cancellationToken);

        var items = listPage.Items.Select(item => new PendingProductResponse
        {
            Id = item.MaSanPham,
            Name = item.Ten,
            Price = item.Gia ?? 0,
            CategoryName = item.TenDanhMuc,
            ShopName = item.TenShop,
            ThumbnailUrl = item.AnhDaiDien,
            SubmittedAt = item.Moc,
            ModerationNotes = item.AnalysisStatus
        }).ToList();

        var paged = new PagedResult<PendingProductResponse>(items, listPage.TotalItems, listPage.Page, ProductReviewRules.PageSize);
        return ServiceResult<PagedResult<PendingProductResponse>>.Ok(paged).ToActionResult(this);
    }

    /// <summary>
    /// Phê duyệt hoặc từ chối sản phẩm.
    /// </summary>
    [HttpPost("{productId:int}/decision")]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MakeDecision(
        int productId,
        [FromBody] ModerationDecisionRequest request,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId))
        {
            return ServiceResult.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính quản trị viên.").ToActionResult(this);
        }

        if (request.Approved)
        {
            var result = await reviewService.ApproveAsync(adminId, productId, cancellationToken);
            if (!result.Success)
            {
                return ServiceResult.Fail(ErrorCodes.BadRequest, result.Error ?? "Không thể duyệt sản phẩm.").ToActionResult(this);
            }

            return ServiceResult.Ok("Phê duyệt sản phẩm thành công.").ToActionResult(this);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < ProductReviewRules.MinReasonLength)
            {
                return ServiceResult.Fail(ErrorCodes.RejectionReasonRequired, $"Lý do từ chối tối thiểu {ProductReviewRules.MinReasonLength} ký tự.").ToActionResult(this);
            }

            var result = await reviewService.RejectAsync(adminId, productId, request.Reason, cancellationToken);
            if (!result.Success)
            {
                return ServiceResult.Fail(ErrorCodes.BadRequest, result.Error ?? "Không thể từ chối sản phẩm.").ToActionResult(this);
            }

            return ServiceResult.Ok("Từ chối sản phẩm thành công.").ToActionResult(this);
        }
    }
}
