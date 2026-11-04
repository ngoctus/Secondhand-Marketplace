using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Models.Enums;

namespace SecondHandMarketplace.Web.Models.Entities;

public class Auction
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Precision(18, 2)] public decimal StartPrice { get; set; }
    [Precision(18, 2)] public decimal BidStep { get; set; }
    [Precision(18, 2)] public decimal CurrentPrice { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AuctionStatus Status { get; set; } = AuctionStatus.Scheduled;

    public string? WinnerId { get; set; }
    public AppUser? Winner { get; set; }

    /// <summary>Chống 2 người bid cùng lúc: EF tự báo lỗi nếu dòng đã bị người khác sửa.</summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public List<Bid> Bids { get; set; } = new();
}
