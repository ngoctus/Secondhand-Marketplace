using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels;
using SecondHandMarketplace.Web.Models.ViewModels.Auctions;

namespace SecondHandMarketplace.Web.Services.Auctions;

public sealed class AuctionService(
    AppDbContext db,
    Microsoft.AspNetCore.SignalR.IHubContext<SecondHandMarketplace.Web.Hubs.AuctionHub>? auctionHub = null,
    SecondHandMarketplace.Web.Services.Notifications.INotificationService? notificationService = null) : IAuctionService
{
    private static SqlParameter Tien(string ten, decimal v) =>
        new(ten, SqlDbType.Decimal) { Value = v, Precision = 18, Scale = 2 };

    private static SqlParameter Gio(string ten, DateTime v) =>
        new(ten, SqlDbType.DateTime2) { Value = v };

    private static IQueryable<AuctionCardVm> ToCard(IQueryable<PhienDauGia> q) => q.Select(p => new AuctionCardVm
    {
        Id = p.MaPhienDauGia,
        ProductId = p.MaSanPham,
        ProductName = p.SanPham.Ten,
        Image = p.SanPham.Anh.Where(a => a.LaAnhChinh).Select(a => a.DuongDan).FirstOrDefault(),
        ShopName = p.SanPham.CuaHang.TenShop,
        StartPrice = p.GiaKhoiDiem,
        Step = p.BuocGia,
        CurrentPrice = p.GiaHienTai,
        BidCount = p.SoLuotDat,
        StartAt = p.ThoiGianBatDau,
        EndAt = p.ThoiGianKetThuc,
        Status = p.TrangThai,
        CancelReason = p.LyDoHuy,
        WinnerName = p.NguoiThang != null ? p.NguoiThang.HoTen : null
    });

    private static void ChuanHoaTrangThai(IEnumerable<AuctionCardVm> items)
    {
        var now = DateTime.UtcNow;
        foreach (var x in items) x.Status = AuctionRules.TrangThaiHienThi(x.Status, x.StartAt, x.EndAt, now);
    }

    private async Task<PagedResult<AuctionCardVm>> PageAsync(IQueryable<PhienDauGia> q, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        var total = await q.CountAsync(ct);
        var items = await ToCard(q).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        ChuanHoaTrangThai(items);
        return new PagedResult<AuctionCardVm> { Items = items, Page = page, PageSize = pageSize, TotalItems = total };
    }

    public async Task<PagedResult<AuctionCardVm>> SearchPublicAsync(string tab, string? q, int page, int pageSize, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var query = db.PhienDauGias.AsNoTracking().Where(p => !p.SanPham.DaXoa && p.SanPham.CuaHang.TrangThai == "ACTIVE");

        if (!string.IsNullOrWhiteSpace(q))
        {
            var k = TextHelper.ChuanHoa(q);
            query = query.Where(p => p.SanPham.ChuoiTimKiem.Contains(k));
        }

        IQueryable<PhienDauGia> sorted = tab switch
        {
            "sap-dien-ra" => query.Where(p => p.TrangThai == "SCHEDULED" && p.ThoiGianBatDau > now).OrderBy(p => p.ThoiGianBatDau),
            "da-ket-thuc" => query.Where(p => p.TrangThai == "ENDED" || p.TrangThai == "CANCELLED" || p.ThoiGianKetThuc <= now)
                                   .OrderByDescending(p => p.ThoiGianKetThuc),
            _ => query.Where(p => (p.TrangThai == "SCHEDULED" || p.TrangThai == "RUNNING")
                                  && p.ThoiGianBatDau <= now && p.ThoiGianKetThuc > now).OrderBy(p => p.ThoiGianKetThuc)
        };
        return await PageAsync(sorted, page, pageSize, ct);
    }

    private async Task<List<BidRowVm>> LoadBidsAsync(int id, int? userId, int take, bool anTen, CancellationToken ct)
    {
        var rows = await db.LuotDatGias.AsNoTracking().Where(l => l.MaPhienDauGia == id)
            .OrderByDescending(l => l.SoTien).ThenByDescending(l => l.MaLuotDatGia).Take(take)
            .Select(l => new { l.MaNguoiDung, l.NguoiDung.HoTen, l.SoTien, l.ThoiGian }).ToListAsync(ct);

        return rows.Select(r => new BidRowVm
        {
            Bidder = r.MaNguoiDung == userId ? "Bạn" : anTen ? HienThi.AnDanhTinh(r.HoTen) : r.HoTen,
            Amount = r.SoTien,
            At = r.ThoiGian,
            IsMine = r.MaNguoiDung == userId
        }).ToList();
    }

    public async Task<AuctionDetailVm?> GetDetailAsync(int id, int? userId, int maxBids = 30, bool anTen = true, CancellationToken ct = default)
    {
        var p = await db.PhienDauGias.AsNoTracking()
            .Include(x => x.SanPham).ThenInclude(s => s.CuaHang)
            .Include(x => x.SanPham).ThenInclude(s => s.Anh)
            .Include(x => x.SanPham).ThenInclude(s => s.DanhMuc)
            .Include(x => x.SanPham).ThenInclude(s => s.TinhTrang)
            .Include(x => x.NguoiThang)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.MaPhienDauGia == id
                                      && !x.SanPham.DaXoa
                                      && x.SanPham.CuaHang.TrangThai == "ACTIVE"
                                      && x.SanPham.CuaHang.NguoiDung.TrangThai == "ACTIVE", ct);
        if (p is null) return null;

        var now = DateTime.UtcNow;
        return new AuctionDetailVm
        {
            Id = p.MaPhienDauGia,
            ProductId = p.MaSanPham,
            ProductName = p.SanPham.Ten,
            Description = p.SanPham.MoTa,
            CategoryName = p.SanPham.DanhMuc?.Ten,
            ConditionName = p.SanPham.TinhTrang?.Ten,
            Images = p.SanPham.Anh.OrderBy(a => a.ThuTu).Select(a => a.DuongDan).ToList(),
            ShopName = p.SanPham.CuaHang.TenShop,
            ListPrice = p.GiaNiemYet,
            StartPrice = p.GiaKhoiDiem,
            Step = p.BuocGia,
            CurrentPrice = p.GiaHienTai,
            MinNextBid = AuctionRules.GiaToiThieu(p.SoLuotDat, p.GiaKhoiDiem, p.GiaHienTai, p.BuocGia),
            BidCount = p.SoLuotDat,
            StartAt = p.ThoiGianBatDau,
            EndAt = p.ThoiGianKetThuc,
            Status = AuctionRules.TrangThaiHienThi(p.TrangThai, p.ThoiGianBatDau, p.ThoiGianKetThuc, now),
            ServerNowUtc = now,
            IsOwner = userId.HasValue && p.SanPham.CuaHang.MaNguoiDung == userId,
            IsLeader = userId.HasValue && p.MaNguoiDanDau == userId,
            WinnerName = p.NguoiThang is null ? null : (anTen ? HienThi.AnDanhTinh(p.NguoiThang.HoTen) : p.NguoiThang.HoTen),
            WinningPrice = p.GiaThang,
            OrderId = p.MaDonHang,
            CancelReason = p.LyDoHuy,
            CancelBy = p.NguoiHuy,
            Bids = await LoadBidsAsync(id, userId, maxBids, anTen, ct)
        };
    }

    public async Task<AuctionStatusDto?> GetStatusAsync(int id, int? userId, CancellationToken ct = default)
    {
        var p = await db.PhienDauGias.AsNoTracking().Where(x => x.MaPhienDauGia == id)
            .Select(x => new { x.TrangThai, x.GiaKhoiDiem, x.BuocGia, x.GiaHienTai, x.SoLuotDat, x.ThoiGianBatDau, x.ThoiGianKetThuc, x.MaNguoiDanDau })
            .FirstOrDefaultAsync(ct);
        if (p is null) return null;

        var now = DateTime.UtcNow;
        var status = AuctionRules.TrangThaiHienThi(p.TrangThai, p.ThoiGianBatDau, p.ThoiGianKetThuc, now);
        var min = AuctionRules.GiaToiThieu(p.SoLuotDat, p.GiaKhoiDiem, p.GiaHienTai, p.BuocGia);
        var bids = await LoadBidsAsync(id, userId, 10, true, ct);

        return new AuctionStatusDto
        {
            Status = status,
            StatusText = HienThi.TrangThaiPhien(status),
            CurrentPrice = p.GiaHienTai,
            CurrentPriceText = HienThi.Tien(p.GiaHienTai),
            BidCount = p.SoLuotDat,
            MinNextBid = min,
            MinNextBidText = HienThi.Tien(min),
            StartAtUtc = HienThi.Iso(p.ThoiGianBatDau),
            EndAtUtc = HienThi.Iso(p.ThoiGianKetThuc),
            ServerNowUtc = HienThi.Iso(now),
            IsLeader = userId.HasValue && p.MaNguoiDanDau == userId,
            Bids = bids.Select(b => new BidDto { Bidder = b.Bidder, AmountText = HienThi.Tien(b.Amount), Time = HienThi.GioGiay(b.At), Mine = b.IsMine }).ToList()
        };
    }

    public Task<int?> GetOpenAuctionIdForProductAsync(int productId, CancellationToken ct = default) =>
        db.PhienDauGias.AsNoTracking()
            .Where(p => p.MaSanPham == productId && (p.TrangThai == "SCHEDULED" || p.TrangThai == "RUNNING"))
            .Select(p => (int?)p.MaPhienDauGia).FirstOrDefaultAsync(ct);

    public async Task<(bool Ok, string? Error)> PlaceBidAsync(int id, int userId, decimal amount, CancellationToken ct = default)
    {
        if (amount <= 0) return (false, "Số tiền đặt giá không hợp lệ.");

        // Báo lỗi sớm ở C#; sp_DatGia kiểm tra lại trong transaction (chốt chặn cuối).
        var thongTin = await db.PhienDauGias.AsNoTracking()
            .Where(p => p.MaPhienDauGia == id)
            .Select(p => new
            {
                SanPhamTrangThai = p.SanPham.TrangThai,
                SanPhamDaXoa = p.SanPham.DaXoa,
                ShopTrangThai = p.SanPham.CuaHang.TrangThai
            })
            .FirstOrDefaultAsync(ct);
        if (thongTin is not null)
        {
            if (thongTin.SanPhamDaXoa || thongTin.SanPhamTrangThai is not ("ACTIVE" or "RESERVED"))
                return (false, "Sản phẩm đã bị gỡ hoặc không còn bán, không thể đặt giá.");
            if (thongTin.ShopTrangThai != "ACTIVE")
                return (false, "Cửa hàng đang bị khóa, không thể đặt giá.");
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_DatGia @MaPhienDauGia, @MaNguoiDung, @SoTien",
                new object[] { new SqlParameter("@MaPhienDauGia", id), new SqlParameter("@MaNguoiDung", userId), Tien("@SoTien", amount) }, ct);
            if (auctionHub is not null)
            {
                try
                {
                    var status = await GetStatusAsync(id, actorId, ct);
                    if (status is not null)
                    {
                        _ = auctionHub.Clients.Group($"auction-{id}").SendAsync("ReceiveBidUpdate", status, ct);
                    }
                }
                catch { }
            }

            return (true, null);
        }
        catch (SqlException ex) { return (false, SqlLoi.ThongBao(ex)); }
    }

    public Task<bool> HasShopAsync(int userId, CancellationToken ct = default) =>
        db.CuaHangs.AsNoTracking().AnyAsync(c => c.MaNguoiDung == userId, ct);

    public async Task<IReadOnlyList<EligibleProductVm>> GetEligibleProductsAsync(int userId, CancellationToken ct = default) =>
        await db.SanPhams.AsNoTracking()
            .Where(s => s.CuaHang.MaNguoiDung == userId && s.CuaHang.TrangThai == "ACTIVE"
                        && s.TrangThai == "ACTIVE" && !s.DaXoa && s.Gia != null
                        && !db.PhienDauGias.Any(p => p.MaSanPham == s.MaSanPham && (p.TrangThai == "SCHEDULED" || p.TrangThai == "RUNNING")))
            .OrderBy(s => s.Ten)
            .Select(s => new EligibleProductVm
            {
                Id = s.MaSanPham,
                Name = s.Ten,
                Price = s.Gia!.Value,
                Image = s.Anh.Where(a => a.LaAnhChinh).Select(a => a.DuongDan).FirstOrDefault()
            }).ToListAsync(ct);

    public Task<PagedResult<AuctionCardVm>> GetSellerAuctionsAsync(int userId, int page, int pageSize, CancellationToken ct = default) =>
        PageAsync(db.PhienDauGias.AsNoTracking().Where(p => p.SanPham.CuaHang.MaNguoiDung == userId)
            .OrderByDescending(p => p.NgayTao), page, pageSize, ct);

    public async Task<(bool Ok, string? Error, int? Id)> CreateAsync(int userId, CreateAuctionVm vm, CancellationToken ct = default)
    {
        if (vm.ProductId is null || vm.StartPrice is null || vm.Step is null || vm.EndAt is null)
            return (false, "Vui lòng nhập đầy đủ thông tin.", null);

        var now = DateTime.UtcNow;
        var batDau = vm.StartAt.HasValue ? HienThi.TuGioVnSangUtc(vm.StartAt.Value) : now;
        if (batDau < now) batDau = now;                       // chọn giờ bắt đầu đã qua thì bắt đầu ngay
        var ketThuc = HienThi.TuGioVnSangUtc(vm.EndAt.Value);

        var pId = new SqlParameter("@MaPhienMoi", SqlDbType.Int) { Direction = ParameterDirection.Output };
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_TaoPhienDauGia @MaNguoiDung, @MaSanPham, @GiaKhoiDiem, @BuocGia, @Bat, @Ket, @MaPhienMoi OUTPUT",
                new object[]
                {
                    new SqlParameter("@MaNguoiDung", userId), new SqlParameter("@MaSanPham", vm.ProductId.Value),
                    Tien("@GiaKhoiDiem", vm.StartPrice.Value), Tien("@BuocGia", vm.Step.Value),
                    Gio("@Bat", batDau), Gio("@Ket", ketThuc), pId
                }, ct);
            return (true, null, (int)pId.Value);
        }
        catch (SqlException ex) { return (false, SqlLoi.ThongBao(ex), null); }
    }

    public async Task<(bool Ok, string? Error)> CancelAsync(int actorId, int id, string? reason, CancellationToken ct = default)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_HuyPhienDauGia @MaPhienDauGia, @MaNguoiDung, @LyDo",
                new object[] { new SqlParameter("@MaPhienDauGia", id), new SqlParameter("@MaNguoiDung", actorId),
                               new SqlParameter("@LyDo", (object?)reason ?? DBNull.Value) }, ct);
            if (auctionHub is not null)
            {
                try
                {
                    var status = await GetStatusAsync(id, actorId, ct);
                    if (status is not null)
                    {
                        _ = auctionHub.Clients.Group($"auction-{id}").SendAsync("ReceiveBidUpdate", status, ct);
                    }
                }
                catch { }
            }

            return (true, null);
        }
        catch (SqlException ex) { return (false, SqlLoi.ThongBao(ex)); }
    }

    public Task<PagedResult<AuctionCardVm>> SearchAdminAsync(string? status, string? q, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.PhienDauGias.AsNoTracking().Where(p => !p.SanPham.DaXoa);
        var statusChuan = status?.Trim().ToUpperInvariant();
        if (statusChuan is "SCHEDULED" or "RUNNING" or "ENDED" or "CANCELLED") query = query.Where(p => p.TrangThai == statusChuan);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var k = q.Trim();
            var kChuan = TextHelper.ChuanHoa(k);
            query = int.TryParse(k, out var idPhien)
                ? query.Where(p => p.MaPhienDauGia == idPhien
                                   || p.SanPham.ChuoiTimKiem.Contains(kChuan)
                                   || p.SanPham.CuaHang.TenShop.Contains(k))
                : query.Where(p => p.SanPham.ChuoiTimKiem.Contains(kChuan) || p.SanPham.CuaHang.TenShop.Contains(k));
        }
        return PageAsync(query.OrderByDescending(p => p.NgayTao), page, pageSize, ct);
    }
}