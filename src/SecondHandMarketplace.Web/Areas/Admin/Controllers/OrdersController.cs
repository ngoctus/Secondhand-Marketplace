using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Models.ViewModels.Orders;
using SecondHandMarketplace.Web.Services.OrderManagement;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

/// <summary>E2: Admin xem toàn bộ đơn và can thiệp (hủy / đổi trạng thái hợp lệ, bắt buộc lý do).</summary>
public sealed class OrdersController(IOrderManagementService orders) : AdminBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AdminOrderFilter filter, CancellationToken ct)
    {
        return View(new AdminOrderListVm
        {
            Filter = filter,
            Result = await orders.SearchAdminOrdersAsync(filter, 15, ct),
            Shops = await orders.GetShopOptionsAsync(ct)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var vm = await orders.GetAdminOrderAsync(id, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string reason, CancellationToken ct)
    {
        var adminId = User.LayNguoiDungId();
        if (adminId is null) return Challenge();

        var (ok, error) = await orders.AdminUpdateStatusAsync(adminId.Value, id, status, reason, ct);
        if (ok) this.ThongBaoThanhCong("Đã cập nhật đơn hàng, người mua và người bán đã được thông báo.");
        else this.ThongBaoLoi(error ?? "Không cập nhật được đơn hàng.");
        return RedirectToAction(nameof(Details), new { id });
    }
}