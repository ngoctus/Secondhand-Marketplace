using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.DTOs.Auctions;

public class CreateAuctionRequest
{
    [Required(ErrorMessage = "Mã sản phẩm không được để trống.")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Giá khởi điểm không được để trống.")]
    [Range(1000, 10000000000, ErrorMessage = "Giá khởi điểm không hợp lệ.")]
    public decimal StartingPrice { get; set; }

    [Required(ErrorMessage = "Bước giá không được để trống.")]
    [Range(1000, 1000000000, ErrorMessage = "Bước giá không hợp lệ.")]
    public decimal StepPrice { get; set; }

    [Required(ErrorMessage = "Thời gian bắt đầu không được để trống.")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "Thời gian kết thúc không được để trống.")]
    public DateTime EndTime { get; set; }
}

public class PlaceBidRequest
{
    [Required(ErrorMessage = "Số tiền đặt giá không được để trống.")]
    [Range(1000, 10000000000, ErrorMessage = "Số tiền đặt giá không hợp lệ.")]
    public decimal Amount { get; set; }
}

public class AuctionBidDto
{
    public int Id { get; set; }
    public int BidderId { get; set; }
    public string BidderName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime BidTime { get; set; }
}

public class AuctionResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductThumbnail { get; set; }
    public decimal StartingPrice { get; set; }
    public decimal StepPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public int TotalBids { get; set; }
    public int? LeadingBidderId { get; set; }
    public string? LeadingBidderName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty; // SCHEDULED, RUNNING, ENDED, CANCELLED
    public int? WinnerId { get; set; }
    public string? WinnerName { get; set; }
    public decimal? WinningPrice { get; set; }
}

public class AuctionDetailResponse : AuctionResponse
{
    public string ProductDescription { get; set; } = string.Empty;
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public List<AuctionBidDto> RecentBids { get; set; } = new();
}
