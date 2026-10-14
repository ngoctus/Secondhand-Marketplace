using Microsoft.AspNetCore.Mvc;
using Web2Hand.Common.Extensions;
using Web2Hand.Services.Auctions;

namespace Web2Hand.Areas.Buyer.Controllers;

public class BidsController : BuyerBaseController
{
    private readonly IAuctionService _auctions;

    public BidsController(IAuctionService auctions) => _auctions = auctions;

    // TODO: Index = danh sách các phiên mình đã tham gia / đang dẫn đầu

    /// <summary>POST /Buyer/Bids/Place  (gọi bằng fetch từ trang chi tiết sản phẩm đấu giá)</summary>
    [HttpPost]
    public async Task<IActionResult> Place(int auctionId, decimal amount)
    {
        var (ok, message) = await _auctions.PlaceBidAsync(auctionId, User.GetUserId(), amount);
        return Json(new { ok, message });
    }
}
