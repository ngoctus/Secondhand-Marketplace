using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Models.ViewModels.Orders;
using SecondHandMarketplace.Web.Services.OrderManagement;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

/// <summary>E1: người bán quản lý đơn gửi tới shop của mình.</summary>
public sealed class SellerOrdersController(IOrderManagementService orders) : UserBaseController
{
    private static readonly string[] TrangThaiHopLe = { "PENDING", "CONFIRMED", "SHIPPING", "DELIVERED", "COMPLETED", "CANCELLED" };

    [HttpGet]
    public async Task<IActionResult> Index(string? status, string? q, int page = 1, CancellationToken ct = default)
    {
        var userId = User.LayNguoiDungId();
        if (userId is null) return Challenge();
        if (!await orders.HasShopAsync(userId.Value, ct)) return YeuCauTaoShop();

        if (status is not null && !TrangThaiHopLe.Contains(status)) status = null;
        return View(new OrderListVm
        {
            Status = status,
            Q = q,
            Result = await orders.SearchSellerOrdersAsync(userId.Value, status, q, page, 10, ct),
            Counts = await orders.CountSellerOrdersByStatusAsync(userId.Value, ct)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var userId = User.LayNguoiDungId();
        if (userId is null) return Challenge();
        var vm = await orders.GetSellerOrderAsync(userId.Value, id, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? reason, CancellationToken ct)
    {
        var userId = User.LayNguoiDungId();
        if (userId is null) return Challenge();

        var (ok, error) = await orders.SellerUpdateStatusAsync(userId.Value, id, status, reason, ct);
        if (ok) this.ThongBaoThanhCong("Đã cập nhật đơn hàng, người mua sẽ nhận được thông báo.");
        else this.ThongBaoLoi(error ?? "Không cập nhật được đơn hàng.");
        return RedirectToAction(nameof(Details), new { id });
    }

    private IActionResult YeuCauTaoShop()
    {
        this.ThongBaoLoi("Bạn cần tạo cửa hàng trước khi quản lý đơn bán.");
        return RedirectToAction("Index", "Shop");
    }
}