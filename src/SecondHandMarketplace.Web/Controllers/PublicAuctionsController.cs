using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Services.Auctions;

namespace SecondHandMarketplace.Web.Controllers;

/// <summary>Trang đấu giá công khai: danh sách, chi tiết, polling trạng thái, đặt giá. Đường dẫn /dau-gia.</summary>
[Route("dau-gia")]
public sealed class PublicAuctionsController(IAuctionService auctions) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string tab = "dang-dien-ra", string? q = null, int page = 1, CancellationToken ct = default)
    {
        ViewBag.Tab = tab;
        ViewBag.Q = q;
        return View(await auctions.SearchPublicAsync(tab, q, page, 12, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var vm = await auctions.GetDetailAsync(id, User.LayNguoiDungId(), ct: ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpGet("{id:int}/trang-thai")]
    public async Task<IActionResult> Status(int id, CancellationToken ct)
    {
        var dto = await auctions.GetStatusAsync(id, User.LayNguoiDungId(), ct);
        return dto is null ? NotFound() : Json(dto);
    }

    [Authorize]
    [HttpPost("{id:int}/dat-gia")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceBid(int id, [FromForm] decimal soTien, CancellationToken ct)
    {
        var userId = User.LayNguoiDungId();
        if (userId is null) return Unauthorized(KetQua.ThatBai("Bạn cần đăng nhập để đặt giá."));

        var (ok, error) = await auctions.PlaceBidAsync(id, userId.Value, soTien, ct);
        if (!ok) return BadRequest(KetQua.ThatBai(error ?? "Không đặt giá được."));

        var status = await auctions.GetStatusAsync(id, userId, ct);
        return Ok(KetQua.ThanhCong("Đặt giá thành công! Bạn đang dẫn đầu.", status));
    }
}