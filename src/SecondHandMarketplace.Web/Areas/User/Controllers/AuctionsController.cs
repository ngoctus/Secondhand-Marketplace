using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Models.ViewModels.Auctions;
using SecondHandMarketplace.Web.Services.Auctions;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

/// <summary>Đấu giá của tôi (người bán): xem phiên, tạo phiên, hủy phiên khi chưa có người đặt.</summary>
public sealed class AuctionsController(IAuctionService auctions) : UserBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var userId = User.LayNguoiDungId();
        if (userId is null) return Challenge();
        if (!await auctions.HasShopAsync(userId.Value, ct)) return YeuCauTaoShop();
        return View(await auctions.GetSellerAuctionsAsync(userId.Value, page, 10, ct));
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? productId, CancellationToken ct)
    {
        var userId = User.LayNguoiDungId();
        if (userId is null) return Challenge();
        if (!await auctions.HasShopAsync(userId.Value, ct)) return YeuCauTaoShop();

        var vm = new CreateAuctionVm { ProductId = productId, EndAt = HienThi.SangGioVn(DateTime.UtcNow).AddDays(3) };
        vm.EndAt = new DateTime(vm.EndAt.Value.Year, vm.EndAt.Value.Month, vm.EndAt.Value.Day, vm.EndAt.Value.Hour, 0, 0);
        vm.Products = (await auctions.GetEligibleProductsAsync(userId.Value, ct)).ToList();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAuctionVm vm, CancellationToken ct)
    {
        var userId = User.LayNguoiDungId();
        if (userId is null) return Challenge();

        if (ModelState.IsValid)
        {
            var (ok, error, _) = await auctions.CreateAsync(userId.Value, vm, ct);
            if (ok)
            {
                this.ThongBaoThanhCong("Đã tạo phiên đấu giá. Sản phẩm được giữ riêng cho phiên này cho tới khi kết thúc.");
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", error ?? "Không tạo được phiên đấu giá.");
        }
        vm.Products = (await auctions.GetEligibleProductsAsync(userId.Value, ct)).ToList();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason, CancellationToken ct)
    {
        var userId = User.LayNguoiDungId();
        if (userId is null) return Challenge();
        var (ok, error) = await auctions.CancelAsync(userId.Value, id, reason, ct);
        if (ok) this.ThongBaoThanhCong("Đã hủy phiên đấu giá, sản phẩm quay lại trạng thái đang bán.");
        else this.ThongBaoLoi(error ?? "Không hủy được phiên.");
        return RedirectToAction(nameof(Index));
    }

    private IActionResult YeuCauTaoShop()
    {
        this.ThongBaoLoi("Bạn cần tạo cửa hàng trước khi dùng tính năng đấu giá.");
        return RedirectToAction("Index", "Shop");
    }
}