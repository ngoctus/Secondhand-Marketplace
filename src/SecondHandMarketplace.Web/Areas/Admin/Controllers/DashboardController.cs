using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.Dashboard;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public sealed class DashboardController(IDashboardService dashboard, AppDbContext db) : AdminBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.Shops = await db.CuaHangs.AsNoTracking()
            .Where(x => x.TrangThai == "ACTIVE")
            .OrderBy(x => x.TenShop)
            .Select(x => new { x.MaCuaHang, x.TenShop })
            .ToListAsync(ct);

        ViewBag.Buyers = await db.NguoiDungs.AsNoTracking()
            .Where(x => x.TrangThai == "ACTIVE" && x.VaiTro == "USER")
            .OrderBy(x => x.HoTen)
            .Take(50)
            .Select(x => new { x.MaNguoiDung, x.HoTen, x.SoDienThoai, x.DiaChiMacDinh })
            .ToListAsync(ct);

        return View(await dashboard.GetAdminAsync(ct));
    }

    [HttpGet]
    public async Task<IActionResult> Chart(string mode = "day", CancellationToken ct = default) =>
        Json(await dashboard.GetAdminChartAsync(mode, ct));

    [HttpGet]
    public async Task<IActionResult> ExportCsv(CancellationToken ct)
    {
        var orders = await db.DonHangs.AsNoTracking()
            .Where(x => x.TrangThai == "COMPLETED")
            .OrderBy(x => x.ThoiGianHoanTat ?? x.NgayTao)
            .Select(x => new
            {
                x.MaDon,
                CompletedAt = x.ThoiGianHoanTat ?? x.NgayTao,
                Shop = x.CuaHang.TenShop,
                Buyer = x.NguoiDung.HoTen,
                x.TongTien
            })
            .ToListAsync(ct);

        var rows = new List<IEnumerable<string?>>
        {
            new[] { "Mã đơn", "Ngày hoàn tất (UTC)", "Cửa hàng", "Người mua", "Doanh thu (VND)", "Lợi nhuận sàn 5% (VND)" }
        };
        rows.AddRange(orders.Select(x => (IEnumerable<string?>)new[]
        {
            x.MaDon,
            x.CompletedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            x.Shop,
            x.Buyer,
            CsvExport.Money(x.TongTien),
            CsvExport.Money(Math.Round(x.TongTien * 0.05m, 0))
        }));

        return CsvExport.Create($"remarket-admin-revenue-{DateTime.UtcNow:yyyyMMdd}.csv", rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrder(int shopId, int buyerId, string productName, decimal amount, string status = "COMPLETED", CancellationToken ct = default)
    {
        productName = (productName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(productName) || amount <= 0)
        {
            TempData["Loi"] = "Vui lòng nhập tên sản phẩm và số tiền hợp lệ (> 0).";
            return RedirectToAction(nameof(Index));
        }

        var shop = await db.CuaHangs.AsNoTracking().FirstOrDefaultAsync(x => x.MaCuaHang == shopId, ct)
                   ?? await db.CuaHangs.AsNoTracking().FirstOrDefaultAsync(ct);
        var buyer = await db.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(x => x.MaNguoiDung == buyerId, ct)
                    ?? await db.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(x => x.VaiTro == "USER", ct);

        if (shop is null || buyer is null)
        {
            TempData["Loi"] = "Không tìm thấy cửa hàng hoặc người mua hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var now = DateTime.UtcNow;
        var sp = await db.SanPhams.FirstOrDefaultAsync(x => x.MaCuaHang == shop.MaCuaHang && x.Ten == productName && !x.DaXoa, ct);
        if (sp is null)
        {
            var danhMucId = await db.DanhMucs.AsNoTracking().Select(x => (int?)x.MaDanhMuc).FirstOrDefaultAsync(ct);
            var tinhTrangId = await db.TinhTrangSanPhams.AsNoTracking().Select(x => (int?)x.MaTinhTrang).FirstOrDefaultAsync(ct);
            sp = new SanPham
            {
                MaCuaHang = shop.MaCuaHang,
                MaDanhMuc = danhMucId,
                MaTinhTrang = tinhTrangId,
                Ten = productName,
                ChuoiTimKiem = TextHelper.ChuanHoa(productName),
                MoTa = productName,
                Gia = amount,
                TrangThai = status == "COMPLETED" ? "SOLD" : "ACTIVE",
                NgayTao = now,
                NgayCapNhat = now
            };
            db.SanPhams.Add(sp);
            await db.SaveChangesAsync(ct);
        }

        var normalizedStatus = status is "PENDING" or "CONFIRMED" or "SHIPPING" or "DELIVERED" or "COMPLETED" or "CANCELLED"
            ? status
            : "COMPLETED";

        var maDon = $"DH{now:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";
        var order = new DonHang
        {
            MaDon = maDon,
            MaNguoiDung = buyer.MaNguoiDung,
            MaCuaHang = shop.MaCuaHang,
            TenNguoiNhan = buyer.HoTen,
            SoDienThoaiNhan = string.IsNullOrWhiteSpace(buyer.SoDienThoai) ? "0901234567" : buyer.SoDienThoai,
            DiaChiNhan = string.IsNullOrWhiteSpace(buyer.DiaChiMacDinh) ? "TP. Hồ Chí Minh" : buyer.DiaChiMacDinh,
            TienHang = amount,
            PhiVanChuyen = 0m,
            TongTien = amount,
            PhuongThucThanhToan = "COD",
            TrangThaiThanhToan = normalizedStatus == "COMPLETED" ? "PAID" : "UNPAID",
            TrangThai = normalizedStatus,
            HinhThucNhan = "GIAO_HANG",
            ThoiGianHoanTat = normalizedStatus == "COMPLETED" ? now : null,
            NgayTao = now,
            NgayCapNhat = now
        };
        db.DonHangs.Add(order);
        await db.SaveChangesAsync(ct);

        db.ChiTietDonHangs.Add(new ChiTietDonHang
        {
            MaDonHang = order.MaDonHang,
            MaSanPham = sp.MaSanPham,
            TenSanPham = productName,
            Gia = amount,
            DangGiuCho = normalizedStatus is "PENDING" or "CONFIRMED" or "SHIPPING" or "DELIVERED"
        });
        await db.SaveChangesAsync(ct);

        TempData["ThanhCong"] = $"Đã thêm đơn hàng {maDon} ({HienThi.Tien(amount)}) thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditOrder(int id, string? productName, decimal amount, string status, CancellationToken ct = default)
    {
        var order = await db.DonHangs.FirstOrDefaultAsync(x => x.MaDonHang == id, ct);
        if (order is null)
        {
            TempData["Loi"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(Index));
        }

        if (amount <= 0)
        {
            TempData["Loi"] = "Số tiền phải lớn hơn 0.";
            return RedirectToAction(nameof(Index));
        }

        var normalizedStatus = status is "PENDING" or "CONFIRMED" or "SHIPPING" or "DELIVERED" or "COMPLETED" or "CANCELLED"
            ? status
            : order.TrangThai;

        order.TienHang = amount;
        order.PhiVanChuyen = 0m;
        order.TongTien = amount;
        order.TrangThai = normalizedStatus;
        if (normalizedStatus == "COMPLETED")
        {
            order.TrangThaiThanhToan = "PAID";
            order.ThoiGianHoanTat ??= DateTime.UtcNow;
        }
        order.NgayCapNhat = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        if (db.Database.IsRelational())
        {
            var cleanName = string.IsNullOrWhiteSpace(productName) ? null : productName.Trim();
            await db.Database.ExecuteSqlRawAsync("DISABLE TRIGGER trg_ChiTietDonHang_KiemTra ON dbo.ChiTietDonHang;", ct);
            try
            {
                if (cleanName is not null)
                {
                    await db.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE dbo.ChiTietDonHang SET Gia = {amount}, TenSanPham = {cleanName} WHERE MaDonHang = {id}", ct);
                }
                else
                {
                    await db.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE dbo.ChiTietDonHang SET Gia = {amount} WHERE MaDonHang = {id}", ct);
                }
            }
            finally
            {
                await db.Database.ExecuteSqlRawAsync("ENABLE TRIGGER trg_ChiTietDonHang_KiemTra ON dbo.ChiTietDonHang;", ct);
            }
        }

        TempData["ThanhCong"] = $"Đã cập nhật đơn hàng {order.MaDon}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOrder(int id, CancellationToken ct = default)
    {
        var order = await db.DonHangs.AsNoTracking().FirstOrDefaultAsync(x => x.MaDonHang == id, ct);
        if (order is null)
        {
            TempData["Loi"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(Index));
        }

        if (db.Database.IsRelational())
        {
            await db.Database.ExecuteSqlRawAsync("DISABLE TRIGGER trg_ChiTietDonHang_KiemTra ON dbo.ChiTietDonHang;", ct);
            try
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.DanhGia WHERE MaDonHang = {id}", ct);
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.LichSuTranhChap WHERE MaTranhChap IN (SELECT MaTranhChap FROM dbo.TranhChap WHERE MaDonHang = {id})", ct);
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.AnhMinhChungTranhChap WHERE MaTranhChap IN (SELECT MaTranhChap FROM dbo.TranhChap WHERE MaDonHang = {id})", ct);
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.TranhChap WHERE MaDonHang = {id}", ct);
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.ThanhToan WHERE MaDonHang = {id}", ct);
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.LichSuTrangThaiDonHang WHERE MaDonHang = {id}", ct);
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.ChiTietDonHang WHERE MaDonHang = {id}", ct);
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.DonHang WHERE MaDonHang = {id}", ct);
            }
            finally
            {
                await db.Database.ExecuteSqlRawAsync("ENABLE TRIGGER trg_ChiTietDonHang_KiemTra ON dbo.ChiTietDonHang;", ct);
            }
        }
        else
        {
            var entity = await db.DonHangs.FindAsync([id], ct);
            if (entity is not null)
            {
                db.DonHangs.Remove(entity);
                await db.SaveChangesAsync(ct);
            }
        }

        TempData["ThanhCong"] = $"Đã xóa đơn hàng {order.MaDon}.";
        return RedirectToAction(nameof(Index));
    }
}
