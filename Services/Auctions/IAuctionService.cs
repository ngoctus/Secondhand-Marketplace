namespace Web2Hand.Services.Auctions;

public interface IAuctionService
{
    /// <summary>Đặt giá. Trả về (thành công?, thông báo để hiện cho người dùng).</summary>
    Task<(bool Ok, string Message)> PlaceBidAsync(int auctionId, string bidderId, decimal amount);

    /// <summary>Được AuctionJob gọi định kỳ: mở phiên đến giờ, đóng phiên hết giờ, chọn người thắng.</summary>
    Task RefreshStatusesAsync();

    // TODO (Seller): CreateAsync, CancelAsync
}
