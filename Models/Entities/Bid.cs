using Microsoft.EntityFrameworkCore;

namespace SecondHandMarketplace.Web.Models.Entities;

public class Bid
{
    public int Id { get; set; }
    public int AuctionId { get; set; }
    public Auction Auction { get; set; } = null!;
    public string BidderId { get; set; } = string.Empty;
    public AppUser Bidder { get; set; } = null!;

    [Precision(18, 2)]
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
