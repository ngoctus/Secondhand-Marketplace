using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.ViewModels.Auctions;

public sealed class AuctionCardVm
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string? Image { get; set; }
    public string ShopName { get; set; } = "";
    public decimal StartPrice { get; set; }
    public decimal Step { get; set; }
    public decimal CurrentPrice { get; set; }
    public int BidCount { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = "";
    public string? CancelReason { get; set; }
    public string? WinnerName { get; set; }
}

public sealed class BidRowVm
{
    public string Bidder { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime At { get; set; }
    public bool IsMine { get; set; }
}

public sealed class AuctionDetailVm
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string? Description { get; set; }
    public string? CategoryName { get; set; }
    public string? ConditionName { get; set; }
    public List<string> Images { get; set; } = new();
    public string ShopName { get; set; } = "";
    public decimal ListPrice { get; set; }
    public decimal StartPrice { get; set; }
    public decimal Step { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MinNextBid { get; set; }
    public int BidCount { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = "";
    public DateTime ServerNowUtc { get; set; }
    public bool IsOwner { get; set; }
    public bool IsLeader { get; set; }
    public string? WinnerName { get; set; }
    public decimal? WinningPrice { get; set; }
    public int? OrderId { get; set; }
    public string? CancelReason { get; set; }
    public string? CancelBy { get; set; }
    public List<BidRowVm> Bids { get; set; } = new();
}

public sealed class BidDto
{
    public string Bidder { get; set; } = "";
    public string AmountText { get; set; } = "";
    public string Time { get; set; } = "";
    public bool Mine { get; set; }
}

/// <summary>JSON cho polling của auction.js.</summary>
public sealed class AuctionStatusDto
{
    public string Status { get; set; } = "";
    public string StatusText { get; set; } = "";
    public decimal CurrentPrice { get; set; }
    public string CurrentPriceText { get; set; } = "";
    public int BidCount { get; set; }
    public decimal MinNextBid { get; set; }
    public string MinNextBidText { get; set; } = "";
    public string StartAtUtc { get; set; } = "";
    public string EndAtUtc { get; set; } = "";
    public string ServerNowUtc { get; set; } = "";
    public bool IsLeader { get; set; }
    public List<BidDto> Bids { get; set; } = new();
}

public sealed class EligibleProductVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string? Image { get; set; }
}

public sealed class CreateAuctionVm
{
    [Required(ErrorMessage = "Vui lòng chọn sản phẩm.")]
    [Display(Name = "Sản phẩm")]
    public int? ProductId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá khởi điểm.")]
    [Range(1000, 1000000000, ErrorMessage = "Giá khởi điểm từ 1.000đ đến 1.000.000.000đ.")]
    [Display(Name = "Giá khởi điểm (đ)")]
    public decimal? StartPrice { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập bước giá.")]
    [Range(1000, 1000000000, ErrorMessage = "Bước giá tối thiểu 1.000đ.")]
    [Display(Name = "Bước giá (đ)")]
    public decimal? Step { get; set; }

    [Display(Name = "Bắt đầu lúc (để trống = bắt đầu ngay)")]
    public DateTime? StartAt { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc.")]
    [Display(Name = "Kết thúc lúc")]
    public DateTime? EndAt { get; set; }

    public List<EligibleProductVm> Products { get; set; } = new();
}