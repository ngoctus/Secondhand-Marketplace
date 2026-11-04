using SecondHandMarketplace.Web.Models.ViewModels;
using SecondHandMarketplace.Web.Models.ViewModels.Auctions;

namespace SecondHandMarketplace.Web.Services.Auctions;

public interface IAuctionService
{
    // Công khai
    Task<PagedResult<AuctionCardVm>> SearchPublicAsync(string tab, string? q, int page, int pageSize, CancellationToken ct = default);
    Task<AuctionDetailVm?> GetDetailAsync(int id, int? userId, int maxBids = 30, bool anTen = true, CancellationToken ct = default);
    Task<AuctionStatusDto?> GetStatusAsync(int id, int? userId, CancellationToken ct = default);
    Task<int?> GetOpenAuctionIdForProductAsync(int productId, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> PlaceBidAsync(int id, int userId, decimal amount, CancellationToken ct = default);

    // Người bán
    Task<bool> HasShopAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<EligibleProductVm>> GetEligibleProductsAsync(int userId, CancellationToken ct = default);
    Task<PagedResult<AuctionCardVm>> GetSellerAuctionsAsync(int userId, int page, int pageSize, CancellationToken ct = default);
    Task<(bool Ok, string? Error, int? Id)> CreateAsync(int userId, CreateAuctionVm vm, CancellationToken ct = default);

    // Người bán (khi chưa có người đặt) và Admin
    Task<(bool Ok, string? Error)> CancelAsync(int actorId, int id, string? reason, CancellationToken ct = default);

    // Admin
    Task<PagedResult<AuctionCardVm>> SearchAdminAsync(string? status, string? q, int page, int pageSize, CancellationToken ct = default);
}