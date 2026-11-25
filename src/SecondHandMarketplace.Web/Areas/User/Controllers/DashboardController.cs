using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Services.Dashboard;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

public sealed class DashboardController(IDashboardService dashboard, SecondHandMarketplace.Web.Data.AppDbContext db) : UserBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = User.LayNguoiDungId();
        if (userId is null) return Challenge();
        var vm = await dashboard.GetSellerAsync(userId.Value, ct);
        if (vm is null)
        {
            this.ThongBaoLoi("Bạn cần tạo cửa hàng để xem thống kê.");
            return RedirectToAction("Index", "Shop");
        }
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Chart(string mode = "day", CancellationToken ct = default)
    {
        var userId = User.LayNguoiDungId();
        if (userId is null) return Unauthorized();
        return Json(await dashboard.GetSellerChartAsync(userId.Value, mode, ct));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(CancellationToken ct)
    {
        var userId = User.LayNguoiDungId();
        if (userId is null) return Challenge();
        var shopId = await db.CuaHangs.AsNoTracking()
            .Where(x => x.MaNguoiDung == userId.Value)
            .Select(x => (int?)x.MaCuaHang)
            .FirstOrDefaultAsync(ct);
        if (shopId is null) return NotFound();

        var orders = await db.DonHangs.AsNoTracking()
            .Where(x => x.MaCuaHang == shopId && x.TrangThai == "COMPLETED")
            .OrderBy(x => x.ThoiGianHoanTat ?? x.NgayTao)
            .Select(x => new
            {
                x.MaDon,
                CompletedAt = x.ThoiGianHoanTat ?? x.NgayTao,
                x.TongTien,
                x.PhuongThucThanhToan
            })
            .ToListAsync(ct);

        var rows = new List<IEnumerable<string?>>
        {
            new[] { "Mã đơn", "Ngày hoàn tất (UTC)", "Doanh thu (VND)", "Phương thức thanh toán" }
        };
        rows.AddRange(orders.Select(x => (IEnumerable<string?>)new[]
        {
            x.MaDon,
            x.CompletedAt.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            CsvExport.Money(x.TongTien),
            HienThi.PhuongThucThanhToan(x.PhuongThucThanhToan)
        }));

        return CsvExport.Create($"remarket-shop-revenue-{DateTime.UtcNow:yyyyMMdd}.csv", rows);
    }
}
