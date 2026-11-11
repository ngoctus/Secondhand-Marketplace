using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Reports;
using SecondHandMarketplace.Web.Services.Accounts;
using SecondHandMarketplace.Web.Services.AuditLogs;
using SecondHandMarketplace.Web.Services.Notifications;
using SecondHandMarketplace.Web.Services.Shops;

namespace SecondHandMarketplace.Web.Services.Reports;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;
    private readonly IUserAdminService _userAdmin;
    private readonly IShopService _shopService;
    private readonly IAuditLogService _audit;
    private readonly INotificationService _notifications;

    public ReportService(
        AppDbContext db,
        IUserAdminService userAdmin,
        IShopService shopService,
        IAuditLogService audit,
        INotificationService notifications)
    {
        _db = db;
        _userAdmin = userAdmin;
        _shopService = shopService;
        _audit = audit;
        _notifications = notifications;
    }

    public async Task<(bool ThanhCong, string? ThongBao)> TaoBaoCaoAsync(int maNguoiBaoCao, TaoBaoCaoViewModel vm)
    {
        // 1. Kiểm tra validation độ dài và nội dung
        if (string.IsNullOrWhiteSpace(vm.LyDo))
        {
            return (false, "Vui lòng nhập lý do báo cáo.");
        }
        var lyDoTrim = vm.LyDo.Trim();
        if (lyDoTrim.Length > 255)
        {
            return (false, "Lý do báo cáo không được vượt quá 255 ký tự.");
        }
        var moTaTrim = vm.MoTa?.Trim();
        if (moTaTrim != null && moTaTrim.Length > 1000)
        {
            return (false, "Mô tả chi tiết không được vượt quá 1000 ký tự.");
        }

        // Chuẩn hóa loại đối tượng thành chữ in hoa trước khi so sánh chống trùng
        var loaiDoiTuong = (vm.LoaiDoiTuong ?? string.Empty).Trim().ToUpperInvariant();
        if (loaiDoiTuong != "PRODUCT" && loaiDoiTuong != "USER" && loaiDoiTuong != "SHOP")
        {
            return (false, "Loại đối tượng báo cáo không hợp lệ.");
        }

        // 2. Chống gửi trùng khi đang có báo cáo PENDING
        var daTonTai = await _db.BaoCaos.AnyAsync(x =>
            x.MaNguoiBaoCao == maNguoiBaoCao &&
            x.LoaiDoiTuong == loaiDoiTuong &&
            x.MaDoiTuong == vm.MaDoiTuong &&
            x.TrangThai == "PENDING");

        if (daTonTai)
        {
            return (false, "Bạn đã gửi báo cáo cho đối tượng này và đang chờ ban quản trị xử lý.");
        }

        // 3. Kiểm tra đối tượng báo cáo tồn tại
        switch (loaiDoiTuong)
        {
            case "PRODUCT":
                var sp = await _db.SanPhams.FindAsync(vm.MaDoiTuong);
                if (sp == null || sp.DaXoa) return (false, "Sản phẩm không tồn tại.");
                var chuShop = await _db.CuaHangs.AsNoTracking()
                    .Where(c => c.MaCuaHang == sp.MaCuaHang)
                    .Select(c => c.MaNguoiDung)
                    .FirstOrDefaultAsync();
                if (chuShop == maNguoiBaoCao) return (false, "Không thể tự báo cáo sản phẩm của cửa hàng mình.");
                break;
            case "USER":
                var nd = await _db.NguoiDungs.FindAsync(vm.MaDoiTuong);
                if (nd == null) return (false, "Người dùng không tồn tại.");
                if (nd.MaNguoiDung == maNguoiBaoCao) return (false, "Không thể tự báo cáo chính mình.");
                break;
            case "SHOP":
                var ch = await _db.CuaHangs.FindAsync(vm.MaDoiTuong);
                if (ch == null) return (false, "Cửa hàng không tồn tại.");
                if (ch.MaNguoiDung == maNguoiBaoCao) return (false, "Không thể tự báo cáo cửa hàng của mình.");
                break;
        }

        // 4. Tạo bản ghi báo cáo
        var baoCao = new BaoCao
        {
            LoaiDoiTuong = loaiDoiTuong,
            MaDoiTuong = vm.MaDoiTuong,
            MaNguoiBaoCao = maNguoiBaoCao,
            LyDo = lyDoTrim,
            MoTa = moTaTrim,
            TrangThai = "PENDING",
            NgayTao = DateTime.UtcNow
        };

        try
        {
            _db.BaoCaos.Add(baoCao);
            await _db.SaveChangesAsync();
            return (true, "Đã gửi báo cáo vi phạm thành công. Ban quản trị sẽ sớm xem xét.");
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                return (false, "Bạn đã gửi báo cáo cho đối tượng này và đang chờ ban quản trị xử lý.");
            }
            return (false, SqlLoi.ThongBao(ex));
        }
    }

    public async Task<List<BaoCaoItemViewModel>> LayLichSuBaoCaoCuaToiAsync(int maNguoiBaoCao)
    {
        var rawList = await _db.BaoCaos
            .Include(x => x.NguoiBaoCao)
            .Include(x => x.NguoiXuLy)
            .Where(x => x.MaNguoiBaoCao == maNguoiBaoCao)
            .OrderByDescending(x => x.NgayTao)
            .ToListAsync();

        return await NapTenDoiTuongVaMapVm(rawList);
    }

    public async Task<AdminReportListViewModel> LayDanhSachBaoCaoAdminAsync(string? trangThai, string? loaiDoiTuong, int page = 1, int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _db.BaoCaos
            .Include(x => x.NguoiBaoCao)
            .Include(x => x.NguoiXuLy)
            .AsQueryable();

        var ttChuan = trangThai?.Trim().ToUpperInvariant();
        if (ttChuan is "PENDING" or "RESOLVED" or "DISMISSED")
        {
            query = query.Where(x => x.TrangThai == ttChuan);
        }

        var loaiChuan = loaiDoiTuong?.Trim().ToUpperInvariant();
        if (loaiChuan is "PRODUCT" or "USER" or "SHOP")
        {
            query = query.Where(x => x.LoaiDoiTuong == loaiChuan);
        }

        var totalItems = await query.CountAsync();
        var rawList = await query
            .OrderByDescending(x => x.NgayTao)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = await NapTenDoiTuongVaMapVm(rawList);
        var tongChuaXuLy = await _db.BaoCaos.CountAsync(x => x.TrangThai == "PENDING");

        return new AdminReportListViewModel
        {
            TrangThaiFilter = trangThai,
            LoaiFilter = loaiDoiTuong,
            Items = items,
            TongChuaXuLy = tongChuaXuLy,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<(bool ThanhCong, string? ThongBao)> XuLyBaoCaoAdminAsync(int maBaoCao, int adminId, string hanhDong, string? ghiChu)
    {
        var bc = await _db.BaoCaos.FindAsync(maBaoCao);
        if (bc == null) return (false, "Không tìm thấy báo cáo.");
        if (bc.TrangThai != "PENDING") return (false, "Báo cáo này đã được xử lý trước đó.");

        var now = DateTime.UtcNow;

        if (hanhDong == "DISMISS")
        {
            var lyDoBoQua = string.IsNullOrWhiteSpace(ghiChu) ? "Bỏ qua báo cáo (không vi phạm)." : ghiChu.Trim();
            if (lyDoBoQua.Length > 500)
            {
                return (false, "Lý do / Ghi chú xử lý không được vượt quá 500 ký tự.");
            }
            bc.TrangThai = "DISMISSED";
            bc.GhiChuXuLy = lyDoBoQua;
            bc.NguoiXuLyId = adminId;
            bc.NgayXuLy = now;
            await _db.SaveChangesAsync();

            // Ghi Audit Log thành công
            await _audit.LogAsync(adminId, "DISMISS_REPORT", "BaoCao", maBaoCao, "SUCCESS", lyDoBoQua);

            // Gửi thông báo cho người báo cáo
            await _notifications.CreateAsync(bc.MaNguoiBaoCao, NotificationTypes.System,
                "Kết quả xem xét báo cáo",
                $"Báo cáo của bạn đối với {bc.LoaiDoiTuong} #{bc.MaDoiTuong} đã được ban quản trị xem xét và ghi nhận không vi phạm quy định.",
                bc.MaBaoCao);

            return (true, "Đã bỏ qua báo cáo thành công.");
        }
        else if (hanhDong == "RESOLVE")
        {
            using var tx = _db.Database.IsRelational() ? await _db.Database.BeginTransactionAsync() : null;
            var lyDoXuLy = string.IsNullOrWhiteSpace(ghiChu) ? $"Vi phạm quy định theo báo cáo #{bc.MaBaoCao}" : ghiChu.Trim();
            if (lyDoXuLy.Length > 500)
            {
                return (false, "Lý do / Ghi chú xử lý không được vượt quá 500 ký tự.");
            }

            // Thực thi hành động tương ứng đối tượng thông qua service của bên A/C
            // CHỈ đánh dấu RESOLVED khi hành động thành công thực sự!
            if (bc.LoaiDoiTuong == "USER")
            {
                var (okUser, errUser) = await _userAdmin.LockUserAsync(adminId, bc.MaDoiTuong, lyDoXuLy);
                if (!okUser)
                {
                    var userTarget = await _db.NguoiDungs.FindAsync(bc.MaDoiTuong);
                    if (userTarget != null && userTarget.TrangThai == "LOCKED")
                    {
                        lyDoXuLy += " (Người dùng đã bị khóa trước đó)";
                    }
                    else
                    {
                        await _audit.LogAsync(adminId, "RESOLVE_REPORT", "BaoCao", maBaoCao, "FAILED", errUser);
                        return (false, errUser ?? "Không thể khóa tài khoản người dùng.");
                    }
                }

                try
                {
                    await _notifications.CreateAsync(bc.MaDoiTuong, NotificationTypes.System,
                        "Tài khoản của bạn đã bị khóa",
                        $"Tài khoản của bạn đã bị khóa do vi phạm quy định sàn: {lyDoXuLy}");
                }
                catch { /* Best effort */ }
            }
            else if (bc.LoaiDoiTuong == "SHOP")
            {
                var (okShop, errShop) = await _shopService.LockShopAsync(adminId, bc.MaDoiTuong, lyDoXuLy);
                if (!okShop)
                {
                    var shopTarget = await _db.CuaHangs.FindAsync(bc.MaDoiTuong);
                    if (shopTarget != null && shopTarget.TrangThai == "LOCKED")
                    {
                        lyDoXuLy += " (Cửa hàng đã bị khóa trước đó)";
                    }
                    else
                    {
                        await _audit.LogAsync(adminId, "RESOLVE_REPORT", "BaoCao", maBaoCao, "FAILED", errShop);
                        return (false, errShop ?? "Không thể khóa cửa hàng.");
                    }
                }

                try
                {
                    var shop = await _db.CuaHangs.FindAsync(bc.MaDoiTuong);
                    if (shop != null)
                    {
                        await _notifications.CreateAsync(shop.MaNguoiDung, NotificationTypes.System,
                            "Cửa hàng của bạn đã bị khóa",
                            $"Cửa hàng '{shop.TenShop}' đã bị tạm khóa do vi phạm chính sách: {lyDoXuLy}");
                    }
                }
                catch { /* Best effort */ }
            }
            else if (bc.LoaiDoiTuong == "PRODUCT")
            {
                var spTarget = await _db.SanPhams.FindAsync(bc.MaDoiTuong);
                if (spTarget != null && spTarget.TrangThai == "REMOVED")
                {
                    lyDoXuLy += " (Sản phẩm đã ở trạng thái bị gỡ trước đó)";
                }
                else if (_db.Database.IsRelational())
                {
                    try
                    {
                        await _db.Database.ExecuteSqlRawAsync(
                            "EXEC dbo.sp_ChuyenTrangThaiSanPham @MaSanPham, @TrangThaiMoi, @LoaiNguoiThucHien, @NguoiThucHienId, @LyDo",
                            new SqlParameter("@MaSanPham", bc.MaDoiTuong),
                            new SqlParameter("@TrangThaiMoi", "REMOVED"),
                            new SqlParameter("@LoaiNguoiThucHien", "ADMIN"),
                            new SqlParameter("@NguoiThucHienId", adminId),
                            new SqlParameter("@LyDo", lyDoXuLy));
                    }
                    catch (Exception ex)
                    {
                        await _audit.LogAsync(adminId, "RESOLVE_REPORT", "BaoCao", maBaoCao, "FAILED", ex.Message);
                        return (false, "Không thể gỡ sản phẩm: " + SqlLoi.ThongBao(ex));
                    }
                }
                else
                {
                    var sp = await _db.SanPhams.FindAsync(bc.MaDoiTuong);
                    if (sp == null || sp.DaXoa)
                    {
                        await _audit.LogAsync(adminId, "RESOLVE_REPORT", "BaoCao", maBaoCao, "FAILED", "Sản phẩm không tồn tại.");
                        return (false, "Sản phẩm không tồn tại.");
                    }

                    var spCu = sp.TrangThai;
                    sp.TrangThai = "REMOVED";
                    sp.LyDoGo = lyDoXuLy;
                    sp.NgayCapNhat = now;

                    var chiTiets = await _db.ChiTietDonHangs.Where(c => c.MaSanPham == sp.MaSanPham && c.DangGiuCho).ToListAsync();
                    foreach (var ct in chiTiets) ct.DangGiuCho = false;

                    _db.LichSuTrangThaiSanPhams.Add(new LichSuTrangThaiSanPham
                    {
                        MaSanPham = sp.MaSanPham,
                        TrangThaiCu = spCu,
                        TrangThaiMoi = "REMOVED",
                        LoaiNguoiThucHien = "ADMIN",
                        NguoiThucHienId = adminId,
                        LyDo = lyDoXuLy,
                        ThoiGian = now
                    });
                }

                // Gửi thông báo cho chủ shop của sản phẩm bị gỡ
                var product = await _db.SanPhams.Include(p => p.CuaHang).FirstOrDefaultAsync(p => p.MaSanPham == bc.MaDoiTuong);
                if (product?.CuaHang != null)
                {
                    var chuShopId = product.CuaHang.MaNguoiDung;
                    await _notifications.CreateAsync(chuShopId, NotificationTypes.Product,
                        "Sản phẩm bị gỡ bỏ do vi phạm",
                        $"Sản phẩm '{product.Ten}' đã bị ban quản trị gỡ bỏ khỏi sàn. Lý do: {lyDoXuLy}",
                        bc.MaDoiTuong);

                    // Kiểm tra tổng số vi phạm của người bán (báo cáo đã xử lý + vi phạm kiểm duyệt)
                    var countViPham = await _db.LichSuViPhams.CountAsync(v => v.MaNguoiBan == chuShopId);
                    var countBaoCao = await _db.BaoCaos.CountAsync(b => b.TrangThai == "RESOLVED" &&
                        ((b.LoaiDoiTuong == "PRODUCT" && _db.SanPhams.Any(p => p.MaSanPham == b.MaDoiTuong && p.CuaHang.MaNguoiDung == chuShopId)) ||
                         (b.LoaiDoiTuong == "USER" && b.MaDoiTuong == chuShopId) ||
                         (b.LoaiDoiTuong == "SHOP" && _db.CuaHangs.Any(c => c.MaCuaHang == b.MaDoiTuong && c.MaNguoiDung == chuShopId))));

                    var tongViPham = countViPham + countBaoCao + 1; // +1 cho báo cáo hiện tại
                    if (tongViPham >= 3)
                    {
                        var lyDoKhoa = $"Tự động khóa: Đạt {tongViPham} lần vi phạm/bị gỡ do báo cáo và kiểm duyệt (ngưỡng 3 gậy).";
                        await _userAdmin.LockUserAsync(adminId, chuShopId, lyDoKhoa);
                        await _shopService.LockShopAsync(adminId, product.MaCuaHang, lyDoKhoa);
                        await _notifications.CreateAsync(chuShopId, NotificationTypes.System,
                            "Tài khoản của bạn đã bị khóa",
                            $"Tài khoản và cửa hàng của bạn đã bị khóa tự động vì tích lũy {tongViPham} lần vi phạm (đạt ngưỡng 3 gậy). Vui lòng liên hệ ban quản trị để được hỗ trợ.");
                    }
                }
            }

            // Chỉ cập nhật trạng thái báo cáo sau khi hành động đã thành công
            bc.TrangThai = "RESOLVED";
            bc.GhiChuXuLy = lyDoXuLy;
            bc.NguoiXuLyId = adminId;
            bc.NgayXuLy = now;
            await _db.SaveChangesAsync();
            if (tx != null)
            {
                await tx.CommitAsync();
            }

            // Ghi Audit Log xử lý báo cáo thành công
            await _audit.LogAsync(adminId, "RESOLVE_REPORT", "BaoCao", maBaoCao, "SUCCESS", lyDoXuLy);

            // Gửi thông báo cảm ơn người báo cáo
            await _notifications.CreateAsync(bc.MaNguoiBaoCao, NotificationTypes.System,
                "Báo cáo của bạn đã được xử lý",
                $"Ban quản trị đã tiến hành xử lý vi phạm đối với {bc.LoaiDoiTuong} #{bc.MaDoiTuong} theo báo cáo của bạn. Cảm ơn sự đóng góp của bạn!",
                bc.MaBaoCao);

            return (true, "Đã cập nhật xử lý báo cáo thành công.");
        }
        else
        {
            return (false, "Hành động xử lý không hợp lệ.");
        }
    }

    private async Task<List<BaoCaoItemViewModel>> NapTenDoiTuongVaMapVm(List<BaoCao> list)
    {
        var productIds = list.Where(x => x.LoaiDoiTuong == "PRODUCT").Select(x => x.MaDoiTuong).Distinct().ToList();
        var userIds = list.Where(x => x.LoaiDoiTuong == "USER").Select(x => x.MaDoiTuong).Distinct().ToList();
        var shopIds = list.Where(x => x.LoaiDoiTuong == "SHOP").Select(x => x.MaDoiTuong).Distinct().ToList();

        var products = await _db.SanPhams.Where(p => productIds.Contains(p.MaSanPham)).ToDictionaryAsync(p => p.MaSanPham, p => p.Ten);
        var users = await _db.NguoiDungs.Where(u => userIds.Contains(u.MaNguoiDung)).ToDictionaryAsync(u => u.MaNguoiDung, u => u.HoTen);
        var shops = await _db.CuaHangs.Where(s => shopIds.Contains(s.MaCuaHang)).ToDictionaryAsync(s => s.MaCuaHang, s => s.TenShop);

        return list.Select(x =>
        {
            var tenDoiTuong = x.LoaiDoiTuong switch
            {
                "PRODUCT" => products.TryGetValue(x.MaDoiTuong, out var tenSp) ? tenSp : $"Sản phẩm #{x.MaDoiTuong}",
                "USER" => users.TryGetValue(x.MaDoiTuong, out var tenUser) ? tenUser : $"Người dùng #{x.MaDoiTuong}",
                "SHOP" => shops.TryGetValue(x.MaDoiTuong, out var tenShop) ? tenShop : $"Cửa hàng #{x.MaDoiTuong}",
                _ => $"#{x.MaDoiTuong}"
            };

            return new BaoCaoItemViewModel
            {
                MaBaoCao = x.MaBaoCao,
                LoaiDoiTuong = x.LoaiDoiTuong,
                MaDoiTuong = x.MaDoiTuong,
                TenDoiTuong = tenDoiTuong,
                MaNguoiBaoCao = x.MaNguoiBaoCao,
                TenNguoiBaoCao = x.NguoiBaoCao?.HoTen ?? $"User #{x.MaNguoiBaoCao}",
                LyDo = x.LyDo,
                MoTa = x.MoTa,
                TrangThai = x.TrangThai,
                NguoiXuLyId = x.NguoiXuLyId,
                TenNguoiXuLy = x.NguoiXuLy?.HoTen,
                GhiChuXuLy = x.GhiChuXuLy,
                NgayTao = x.NgayTao,
                NgayXuLy = x.NgayXuLy
            };
        }).ToList();
    }
}
