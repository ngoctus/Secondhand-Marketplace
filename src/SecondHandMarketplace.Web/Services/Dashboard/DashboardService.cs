using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Dashboard;
using SecondHandMarketplace.Web.Services.OrderManagement;

namespace SecondHandMarketplace.Web.Services.Dashboard;

// Các lớp hứng kết quả từ view (tên thuộc tính trùng tên cột)
public sealed class ThongKeCuaHangRow
{
    public int CuaHangId { get; set; }
    public string TenShop { get; set; } = "";
    public int TongSanPham { get; set; }
    public int SanPhamDangBan { get; set; }
    public int SanPhamDaBan { get; set; }
    public int TongDon { get; set; }
    public int DonHoanTat { get; set; }
    public decimal DoanhThu { get; set; }
}

public sealed class ThongKeNguoiBanRow
{
    public int MaNguoiBan { get; set; }
    public string HoTen { get; set; } = "";
    public string TenCuaHang { get; set; } = "";
    public int TongSanPham { get; set; }
    public int SanPhamDaBan { get; set; }
    public int TongDonHang { get; set; }
    public int DonHangHoanTat { get; set; }
    public decimal DoanhThu { get; set; }
}

public sealed class ThongKeHeThongRow
{
    public int TongNguoiDung { get; set; }
    public int TongCuaHang { get; set; }
    public int TongSanPham { get; set; }
    public int SanPhamChoDuyet { get; set; }
    public int TongDonHang { get; set; }
    public int DonHoanTat { get; set; }
    public decimal DoanhThu { get; set; }
}

public sealed class DashboardService(AppDbContext db, IConfiguration config, ILogger<DashboardService> log) : IDashboardService
{
    // Chốt với D tên bảng/cột/giá trị "chưa xử lý". Khi D đã tạo bảng, chỉ cần đặt Dashboard:PendingReportsSql
    // trong appsettings (câu SELECT trả về 1 cột tên Value) mà không phải sửa code.
    private const string DemBaoCaoMacDinh =
        "SELECT COUNT(*) AS Value FROM dbo.BaoCao WHERE TrangThai = 'PENDING'";

    public async Task<SellerDashboardVm?> GetSellerAsync(int userId, CancellationToken ct = default)
    {
        var shop = await db.CuaHangs.AsNoTracking().Where(c => c.MaNguoiDung == userId)
            .Select(c => new { c.MaCuaHang, c.TenShop }).FirstOrDefaultAsync(ct);
        if (shop is null) return null;

        var shopId = shop.MaCuaHang;
        var thongKeShop = (await db.Database.SqlQuery<ThongKeCuaHangRow>(
            $"SELECT * FROM dbo.vw_ThongKeCuaHang WHERE CuaHangId = {shopId}").ToListAsync(ct)).FirstOrDefault();
        var thongKeBan = (await db.Database.SqlQuery<ThongKeNguoiBanRow>(
            $"SELECT * FROM dbo.vw_ThongKeNguoiBan WHERE MaNguoiBan = {userId}").ToListAsync(ct)).FirstOrDefault();

        var theoTrangThai = await db.DonHangs.AsNoTracking().Where(d => d.MaCuaHang == shopId)
            .GroupBy(d => d.TrangThai).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);

        var top = await (from ct2 in db.ChiTietDonHangs.AsNoTracking()
                         join dh in db.DonHangs.AsNoTracking() on ct2.MaDonHang equals dh.MaDonHang
                         where dh.MaCuaHang == shopId && dh.TrangThai == "COMPLETED"
                         group ct2 by new { ct2.MaSanPham, ct2.TenSanPham } into g
                         orderby g.Sum(x => x.Gia) descending
                         select new TopProductVm { ProductId = g.Key.MaSanPham, Name = g.Key.TenSanPham, Revenue = g.Sum(x => x.Gia) })
            .Take(5).ToListAsync(ct);

        var phien = await db.PhienDauGias.AsNoTracking()
            .CountAsync(p => p.SanPham.MaCuaHang == shopId && (p.TrangThai == "SCHEDULED" || p.TrangThai == "RUNNING"), ct);

        return new SellerDashboardVm
        {
            ShopName = shop.TenShop,
            TongSanPham = thongKeShop?.TongSanPham ?? 0,
            SanPhamDangBan = thongKeShop?.SanPhamDangBan ?? 0,
            SanPhamDaBan = thongKeShop?.SanPhamDaBan ?? 0,
            TongDon = thongKeBan?.TongDonHang ?? 0,
            DonHoanTat = thongKeBan?.DonHangHoanTat ?? 0,
            DoanhThu = thongKeBan?.DoanhThu ?? 0,
            DonChoXuLy = theoTrangThai.GetValueOrDefault("PENDING"),
            PhienDangMo = phien,
            DonTheoTrangThai = theoTrangThai,
            SanPhamBanChay = top
        };
    }

    public async Task<AdminDashboardVm> GetAdminAsync(CancellationToken ct = default)
    {
        var tk = (await db.Database.SqlQuery<ThongKeHeThongRow>($"SELECT * FROM dbo.vw_ThongKeHeThong").ToListAsync(ct)).FirstOrDefault()
                 ?? new ThongKeHeThongRow();

        var theoTrangThai = await db.DonHangs.AsNoTracking().GroupBy(d => d.TrangThai)
            .Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);

        var moiNhat = await OrderManagementService.Rows(
            db.DonHangs.AsNoTracking().OrderByDescending(d => d.NgayTao).Take(6)).ToListAsync(ct);

        return new AdminDashboardVm
        {
            TongNguoiDung = tk.TongNguoiDung,
            TongCuaHang = tk.TongCuaHang,
            TongSanPham = tk.TongSanPham,
            SanPhamChoDuyet = tk.SanPhamChoDuyet,
            TongDonHang = tk.TongDonHang,
            DonHoanTat = tk.DonHoanTat,
            DoanhThu = tk.DoanhThu,
            BaoCaoChuaXuLy = await DemBaoCaoAsync(ct),
            PhienDangMo = await db.PhienDauGias.AsNoTracking().CountAsync(p => p.TrangThai == "SCHEDULED" || p.TrangThai == "RUNNING", ct),
            DonTheoTrangThai = theoTrangThai,
            DonMoiNhat = moiNhat
        };
    }

    private async Task<int?> DemBaoCaoAsync(CancellationToken ct)
    {
        var sql = config["Dashboard:PendingReportsSql"] ?? DemBaoCaoMacDinh;
        try
        {
            return await db.Database.SqlQueryRaw<int>(sql).SingleAsync(ct);
        }
        catch (SqlException ex) when (ex.Number is 207 or 208)   // 207: sai tên cột, 208: chưa có bảng
        {
            log.LogWarning("Chưa đếm được báo cáo chưa xử lý, bảng/cột báo cáo chưa có hoặc khác tên ({Msg}). Chỉnh Dashboard:PendingReportsSql trong appsettings.", ex.Message);
            return null;   // giao diện hiển thị "—"
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Lỗi khi đếm báo cáo chưa xử lý.");
            return null;
        }
    }

    public Task<ChartDataVm> GetSellerChartAsync(int userId, string mode, CancellationToken ct = default) =>
        BuildChartAsync(db.DonHangs.AsNoTracking().Where(d => d.CuaHang.MaNguoiDung == userId), mode, ct);

    public Task<ChartDataVm> GetAdminChartAsync(string mode, CancellationToken ct = default) =>
        BuildChartAsync(db.DonHangs.AsNoTracking(), mode, ct);

    /// <summary>mode = "day" (30 ngày gần nhất) hoặc "month" (12 tháng gần nhất). Doanh thu tính theo ngày hoàn tất, số đơn theo ngày tạo.</summary>
    private static async Task<ChartDataVm> BuildChartAsync(IQueryable<DonHang> q, string mode, CancellationToken ct)
    {
        var theoThang = mode == "month";
        var nowVn = HienThi.SangGioVn(DateTime.UtcNow);
        var batDauVn = theoThang ? new DateTime(nowVn.Year, nowVn.Month, 1).AddMonths(-11) : nowVn.Date.AddDays(-29);
        var batDauUtc = HienThi.TuGioVnSangUtc(batDauVn);

        var soCot = theoThang ? 12 : 30;
        var keys = Enumerable.Range(0, soCot).Select(i => theoThang ? batDauVn.AddMonths(i) : batDauVn.AddDays(i)).ToList();

        DateTime Bucket(DateTime utc)
        {
            var v = HienThi.SangGioVn(utc);
            return theoThang ? new DateTime(v.Year, v.Month, 1) : v.Date;
        }

        var hoanTat = await q.Where(d => d.TrangThai == "COMPLETED" && d.ThoiGianHoanTat >= batDauUtc)
            .Select(d => new { T = d.ThoiGianHoanTat!.Value, d.TongTien }).ToListAsync(ct);
        var taoMoi = await q.Where(d => d.NgayTao >= batDauUtc).Select(d => d.NgayTao).ToListAsync(ct);

        var doanhThu = hoanTat.GroupBy(x => Bucket(x.T)).ToDictionary(g => g.Key.Date, g => g.Sum(x => x.TongTien));
        var soDon = taoMoi.GroupBy(Bucket).ToDictionary(g => g.Key.Date, g => g.Count());

        return new ChartDataVm
        {
            Labels = keys.Select(k => k.ToString(theoThang ? "MM/yyyy" : "dd/MM")).ToList(),
            Revenue = keys.Select(k => doanhThu.GetValueOrDefault(k.Date)).ToList(),
            Orders = keys.Select(k => soDon.GetValueOrDefault(k.Date)).ToList()
        };
    }
}