using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.DTOs.Auctions;
using SecondHandMarketplace.Web.Models.ViewModels.Auctions;
using SecondHandMarketplace.Web.Services.Auctions;

namespace SecondHandMarketplace.Web.Controllers.Api.v1;

/// <summary>
/// Quản lý và tham gia đấu giá trực tuyến.
/// </summary>
[ApiController]
[Route("api/v1/auctions")]
[Produces("application/json")]
public class AuctionsController(IAuctionService auctionService) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách các phiên đấu giá công khai (tab: "dang-dien-ra", "sap-dien-ra", "da-ket-thuc").
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceResult<PagedResult<AuctionResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuctions(
        [FromQuery] string tab = "dang-dien-ra",
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var paged = await auctionService.SearchPublicAsync(tab, q, page, pageSize, ct);

        var items = paged.Items.Select(a => new AuctionResponse
        {
            Id = a.Id,
            ProductId = a.ProductId,
            ProductName = a.ProductName,
            ProductThumbnail = a.Image,
            StartingPrice = a.StartPrice,
            StepPrice = a.Step,
            CurrentPrice = a.CurrentPrice,
            TotalBids = a.BidCount,
            StartTime = a.StartAt,
            EndTime = a.EndAt,
            Status = a.Status
        }).ToList();

        var result = new PagedResult<AuctionResponse>(items, paged.TotalItems, paged.Page, paged.PageSize);
        return ServiceResult<PagedResult<AuctionResponse>>.Ok(result).ToActionResult(this);
    }

    /// <summary>
    /// Xem chi tiết một phiên đấu giá.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceResult<AuctionDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(int id, CancellationToken ct)
    {
        int? userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId) ? parsedId : null;
        var auction = await auctionService.GetDetailAsync(id, userId, ct: ct);

        if (auction is null)
        {
            return ServiceResult<AuctionDetailResponse>.Fail(ErrorCodes.AuctionNotFound, "Không tìm thấy phiên đấu giá.").ToActionResult(this);
        }

        var detail = new AuctionDetailResponse
        {
            Id = auction.Id,
            ProductId = auction.ProductId,
            ProductName = auction.ProductName,
            ProductThumbnail = auction.Images.FirstOrDefault(),
            ProductDescription = auction.Description ?? string.Empty,
            StartingPrice = auction.StartPrice,
            StepPrice = auction.Step,
            CurrentPrice = auction.CurrentPrice,
            TotalBids = auction.BidCount,
            StartTime = auction.StartAt,
            EndTime = auction.EndAt,
            Status = auction.Status,
            ShopId = 0,
            ShopName = auction.ShopName,
            WinnerName = auction.WinnerName,
            WinningPrice = auction.WinningPrice,
            RecentBids = auction.Bids.Select(b => new AuctionBidDto
            {
                Id = 0,
                BidderId = 0,
                BidderName = b.Bidder,
                Amount = b.Amount,
                BidTime = b.At
            }).ToList()
        };

        return ServiceResult<AuctionDetailResponse>.Ok(detail).ToActionResult(this);
    }

    /// <summary>
    /// Đặt giá đấu cho phiên đấu giá đang diễn ra.
    /// </summary>
    [HttpPost("{id:int}/bid")]
    [Authorize]
    [ProducesResponseType(typeof(ServiceResult<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PlaceBid(int id, [FromBody] PlaceBidRequest request, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var (ok, error) = await auctionService.PlaceBidAsync(id, userId, request.Amount, ct);
        if (!ok)
        {
            return ServiceResult.Fail(ErrorCodes.BidTooLow, error ?? "Không thể đặt giá.").ToActionResult(this);
        }

        var status = await auctionService.GetStatusAsync(id, userId, ct);
        return ServiceResult<object>.Ok(status!, "Đặt giá thành công! Bạn đang dẫn đầu phiên đấu giá.").ToActionResult(this);
    }
}
