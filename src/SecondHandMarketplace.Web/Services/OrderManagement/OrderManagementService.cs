using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels;
using SecondHandMarketplace.Web.Models.ViewModels.Orders;
using SecondHandMarketplace.Web.Services.AuditLogs;

namespace SecondHandMarketplace.Web.Services.OrderManagement;

public sealed class OrderManagementService(AppDbContext db, IAuditLogService audit) : IOrderManagementService
{
    internal static IQueryable<OrderRowVm> Rows(IQueryable<DonHang> q) => q.Select(d => new OrderRowVm
    {
        Id = d.MaDonHang,
        MaDon = d.MaDon,
        Status = d.TrangThai,
        Total = d.TongTien,
        CreatedAt = d.NgayTao,
        ItemCount = d.ChiTiet.Count,
        FirstItem = d.ChiTiet.OrderBy(c => c.MaChiTietDonHang).Select(c => c.TenSanPham).FirstOrDefault(),
        FirstImage = d.ChiTiet.OrderBy(c => c.MaChiTietDonHang).Select(c => c.AnhDaiDien).FirstOrDefault(),
        BuyerId = d.MaNguoiDung,
            BuyerName = d.NguoiDung.HoTen,
        ShopName = d.CuaHang.TenShop,
        PaymentMethod = d.PhuongThucThanhToan,
        PaymentStatus = d.TrangThaiThanhToan
    });

    private static async Task<PagedResult<OrderRowVm>> PageAsync(IQueryable<DonHang> q, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        var total = await q.CountAsync(ct);
        var items = await Rows(q.OrderByDescending(d => d.NgayTao).ThenByDescending(d => d.MaDonHang))
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<OrderRowVm> { Items = items, Page = page, PageSize = pageSize, TotalItems = total };
    }

    // ---------------------------------------------------------------- E1: người bán
    public Task<bool> HasShopAsync(int userId, CancellationToken ct = default) =>
        db.CuaHangs.AsNoTracking().AnyAsync(c => c.MaNguoiDung == userId, ct);

    public Task<PagedResult<OrderRowVm>> SearchSellerOrdersAsync(int userId, string? status, string? q, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.DonHangs.AsNoTracking().Where(d => d.CuaHang.MaNguoiDung == userId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(d => d.TrangThai == status);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var k = q.Trim();
            query = query.Where(d => d.MaDon.Contains(k) || d.NguoiDung.HoTen.Contains(k) || d.TenNguoiNhan.Contains(k));
        }
        return PageAsync(query, page, pageSize, ct);
    }

    public async Task<Dictionary<string, int>> CountSellerOrdersByStatusAsync(int userId, CancellationToken ct = default) =>
        await db.DonHangs.AsNoTracking().Where(d => d.CuaHang.MaNguoiDung == userId)
            .GroupBy(d => d.TrangThai).Select(g => new { g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.N, ct);

    public Task<OrderDetailVm?> GetSellerOrderAsync(int userId, int orderId, CancellationToken ct = default) =>
        BuildDetailAsync(db.DonHangs.Where(d => d.MaDonHang == orderId && d.CuaHang.MaNguoiDung == userId), ct);

    public async Task<(bool Ok, string? Error)> SellerUpdateStatusAsync(int userId, int orderId, string newStatus, string? reason, CancellationToken ct = default)
    {
        newStatus = (newStatus ?? "").Trim().ToUpperInvariant();
        if (newStatus is not ("CONFIRMED" or "SHIPPING" or "DELIVERED" or "CANCELLED"))
            return (false, "Thao tác không hợp lệ.");
        if (newStatus == "CANCELLED" && string.IsNullOrWhiteSpace(reason))
            return (false, "Vui lòng nhập lý do hủy đơn.");

        var owns = await db.DonHangs.AnyAsync(d => d.MaDonHang == orderId && d.CuaHang.MaNguoiDung == userId, ct);
        if (!owns) return (false, "Không tìm thấy đơn hàng.");

        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_CapNhatTrangThaiDon @MaDonHang, @TrangThaiMoi, @MaNguoiDung, @LyDo",
                new object[]
                {
                    new SqlParameter("@MaDonHang", orderId), new SqlParameter("@TrangThaiMoi", newStatus),
                    new SqlParameter("@MaNguoiDung", userId), new SqlParameter("@LyDo", (object?)reason?.Trim() ?? DBNull.Value)
                }, ct);
        }
        catch (SqlException ex) { return (false, SqlLoi.ThongBao(ex)); }

        await audit.LogAsync(userId, "SELLER_UPDATE_ORDER", "DonHang", orderId, "SUCCESS", reason?.Trim(), newStatus);
        return (true, null);
    }

    // ---------------------------------------------------------------- E2: Admin
    public Task<PagedResult<OrderRowVm>> SearchAdminOrdersAsync(AdminOrderFilter f, int pageSize, CancellationToken ct = default)
    {
        var query = db.DonHangs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(f.Status)) query = query.Where(d => d.TrangThai == f.Status);
        if (f.ShopId is int shopId) query = query.Where(d => d.MaCuaHang == shopId);
        if (f.From is DateTime from)
        {
            var u = HienThi.TuGioVnSangUtc(from.Date);
            query = query.Where(d => d.NgayTao >= u);
        }
        if (f.To is DateTime to)
        {
            var u = HienThi.TuGioVnSangUtc(to.Date.AddDays(1));
            query = query.Where(d => d.NgayTao < u);
        }
        if (!string.IsNullOrWhiteSpace(f.Q))
        {
            var k = f.Q.Trim();
            query = query.Where(d => d.MaDon.Contains(k) || d.NguoiDung.HoTen.Contains(k) || d.TenNguoiNhan.Contains(k));
        }
        return PageAsync(query, f.Page, pageSize, ct);
    }

    public Task<List<ShopOptionVm>> GetShopOptionsAsync(CancellationToken ct = default) =>
        db.CuaHangs.AsNoTracking().OrderBy(c => c.TenShop).Select(c => new ShopOptionVm(c.MaCuaHang, c.TenShop)).ToListAsync(ct);

    public Task<OrderDetailVm?> GetAdminOrderAsync(int orderId, CancellationToken ct = default) =>
        BuildDetailAsync(db.DonHangs.Where(d => d.MaDonHang == orderId), ct);

    public async Task<(bool Ok, string? Error)> AdminUpdateStatusAsync(int adminId, int orderId, string newStatus, string reason, CancellationToken ct = default)
    {
        newStatus = (newStatus ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
            return (false, "Vui lòng nhập lý do can thiệp (tối thiểu 5 ký tự).");
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_AdminCapNhatTrangThaiDon @MaDonHang, @TrangThaiMoi, @QuanTriVienId, @LyDo",
                new object[]
                {
                    new SqlParameter("@MaDonHang", orderId), new SqlParameter("@TrangThaiMoi", newStatus),
                    new SqlParameter("@QuanTriVienId", adminId), new SqlParameter("@LyDo", reason.Trim())
                }, ct);
            return (true, null);   // thủ tục đã tự ghi nhật ký
        }
        catch (SqlException ex) { return (false, SqlLoi.ThongBao(ex)); }
    }

    // ---------------------------------------------------------------- dùng chung
    private async Task<OrderDetailVm?> BuildDetailAsync(IQueryable<DonHang> q, CancellationToken ct)
    {
        var d = await q.AsNoTracking()
            .Include(x => x.NguoiDung)
            .Include(x => x.CuaHang).ThenInclude(c => c.NguoiDung)
            .Include(x => x.ChiTiet)
            .Include(x => x.LichSu)
            .Include(x => x.ThanhToans)
            .AsSplitQuery()
            .FirstOrDefaultAsync(ct);
        if (d is null) return null;

        var ids = d.LichSu.Where(l => l.NguoiThucHienId.HasValue).Select(l => l.NguoiThucHienId!.Value).Distinct().ToList();
        var names = await db.NguoiDungs.AsNoTracking().Where(n => ids.Contains(n.MaNguoiDung))
            .ToDictionaryAsync(n => n.MaNguoiDung, n => n.HoTen, ct);

        return new OrderDetailVm
        {
            Id = d.MaDonHang,
            MaDon = d.MaDon,
            Status = d.TrangThai,
            PaymentMethod = d.PhuongThucThanhToan,
            PaymentStatus = d.TrangThaiThanhToan,
            ItemsTotal = d.TienHang,
            ShippingFee = d.PhiVanChuyen,
            Total = d.TongTien,
            RecipientName = d.TenNguoiNhan,
            RecipientPhone = d.SoDienThoaiNhan,
            RecipientAddress = d.DiaChiNhan,
            BuyerName = d.NguoiDung.HoTen,
            ShopName = d.CuaHang.TenShop,
            SellerName = d.CuaHang.NguoiDung.HoTen,
            CreatedAt = d.NgayTao,
            CancelledBy = d.NguoiHuy,
            CancelReason = d.LyDoHuy,
            CancelledAt = d.ThoiGianHuy,
            Items = d.ChiTiet.OrderBy(c => c.MaChiTietDonHang)
                .Select(c => new OrderItemVm { ProductId = c.MaSanPham, Name = c.TenSanPham, Image = c.AnhDaiDien, Price = c.Gia }).ToList(),
            History = d.LichSu.OrderBy(l => l.ThoiGian).ThenBy(l => l.MaLichSuTrangThaiDonHang)
                .Select(l => new OrderHistoryVm
                {
                    From = l.TrangThaiCu,
                    To = l.TrangThaiMoi,
                    Reason = l.LyDo,
                    At = l.ThoiGian,
                    Actor = l.NguoiThucHienId.HasValue && names.TryGetValue(l.NguoiThucHienId.Value, out var n)
                        ? $"{HienThi.NguoiThucHien(l.LoaiNguoiThucHien)} · {n}"
                        : HienThi.NguoiThucHien(l.LoaiNguoiThucHien)
                }).ToList(),
            Payments = d.ThanhToans.OrderBy(t => t.NgayTao)
                .Select(t => new OrderPaymentVm
                {
                    Method = t.PhuongThucThanhToan,
                    Status = t.TrangThaiThanhToan,
                    Amount = t.SoTien,
                    TransactionCode = t.MaGiaoDich,
                    PaidAt = t.ThoiGianThanhToan
                }).ToList()
        };
    }
}