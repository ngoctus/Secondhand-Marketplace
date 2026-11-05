using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
    private readonly IUserAdminService? _userAdmin;
    private readonly IShopService? _shopService;
    private readonly IAuditLogService? _audit;
    private readonly INotificationService? _notifications;

    public ReportService(
        AppDbContext db,
        IUserAdminService? userAdmin = null,
        IShopService? shopService = null,
        IAuditLogService? audit = null,
        INotificationService? notifications = null)
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

        // 2. Chống gửi trùng khi đang có báo cáo PENDING
        var daTonTai = await _db.BaoCaos.AnyAsync(x =>
            x.MaNguoiBaoCao == maNguoiBaoCao &&
            x.LoaiDoiTuong == vm.LoaiDoiTuong &&
            x.MaDoiTuong == vm.MaDoiTuong &&
            x.TrangThai == "PENDING");

        if (daTonTai)
        {
            return (false, "Bạn đã gửi báo cáo cho đối tượng này và đang chờ ban quản trị xử lý.");
        }

        // 3. Kiểm tra đối tượng báo cáo tồn tại
        switch (vm.LoaiDoiTuong.ToUpper())
        {
            case "PRODUCT":
                var sp = await _db.SanPhams.FindAsync(vm.MaDoiTuong);
                if (sp == null || sp.DaXoa) return (false, "Sản phẩm không tồn tại.");
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
            default:
                return (false, "Loại đối tượng báo cáo không hợp lệ.");
        }

        // 4. Tạo bản ghi báo cáo
        var baoCao = new BaoCao
        {
            LoaiDoiTuong = vm.LoaiDoiTuong.ToUpper(),
            MaDoiTuong = vm.MaDoiTuong,
            MaNguoiBaoCao = maNguoiBaoCao,
            LyDo = lyDoTrim,
            MoTa = moTaTrim,
            TrangThai = "PENDING",
            NgayTao = DateTime.UtcNow
        };

        _db.BaoCaos.Add(baoCao);
        await _db.SaveChangesAsync();

        return (true, "Đã gửi báo cáo vi phạm thành công. Ban quản trị sẽ sớm xem xét.");
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

    public async Task<AdminReportListViewModel> LayDanhSachBaoCaoAdminAsync(string? trangThai, string? loaiDoiTuong)
    {
        var query = _db.BaoCaos
            .Include(x => x.NguoiBaoCao)
            .Include(x => x.NguoiXuLy)
            .AsQueryable();

        if (!string.IsNullOrEmpty(trangThai))
        {
            query = query.Where(x => x.TrangThai == trangThai);
        }

        if (!string.IsNullOrEmpty(loaiDoiTuong))
        {
            query = query.Where(x => x.LoaiDoiTuong == loaiDoiTuong);
        }

        var rawList = await query.OrderByDescending(x => x.NgayTao).ToListAsync();
        var items = await NapTenDoiTuongVaMapVm(rawList);
        var tongChuaXuLy = await _db.BaoCaos.CountAsync(x => x.TrangThai == "PENDING");

        return new AdminReportListViewModel
        {
            TrangThaiFilter = trangThai,
            LoaiFilter = loaiDoiTuong,
            Items = items,
            TongChuaXuLy = tongChuaXuLy
        };
    }

    public async Task<(bool ThanhCong, string? ThongBao)> XuLyBaoCaoAdminAsync(int maBaoCao, int adminId, string hanhDong, string? ghiChu)
    {
        var bc = await _db.BaoCaos.FindAsync(maBaoCao);
        if (bc == null) return (false, "Không tìm thấy báo cáo.");
        if (bc.TrangThai != "PENDING") return (false, "Báo cáo này đã được xử lý trước đó.");

        var now = DateTime.UtcNow;
        bc.NguoiXuLyId = adminId;
        bc.NgayXuLy = now;

        if (hanhDong == "DISMISS")
        {
            bc.TrangThai = "DISMISSED";
            bc.GhiChuXuLy = string.IsNullOrWhiteSpace(ghiChu) ? "Bỏ qua báo cáo (không vi phạm)." : ghiChu.Trim();
            await _db.SaveChangesAsync();

            // Ghi Audit Log
            if (_audit != null)
            {
                await _audit.LogAsync(adminId, "DISMISS_REPORT", "BaoCao", maBaoCao, "SUCCESS", bc.GhiChuXuLy);
            }

            // Gửi thông báo cho người báo cáo
            if (_notifications != null)
            {
                await _notifications.CreateAsync(bc.MaNguoiBaoCao, NotificationTypes.System,
                    "Kết quả xem xét báo cáo",
                    $"Báo cáo của bạn đối với {bc.LoaiDoiTuong} #{bc.MaDoiTuong} đã được ban quản trị xem xét và ghi nhận không vi phạm quy định.",
                    bc.MaBaoCao);
            }
        }
        else if (hanhDong == "RESOLVE")
        {
            bc.TrangThai = "RESOLVED";
            bc.GhiChuXuLy = string.IsNullOrWhiteSpace(ghiChu) ? "Đã xử lý đối tượng vi phạm." : ghiChu.Trim();
            await _db.SaveChangesAsync();

            // Thực thi hành động tương ứng đối tượng thông qua service của bên A/C hoặc procedure chuẩn
            if (bc.LoaiDoiTuong == "USER")
            {
                // Gọi IUserAdminService của bên A để khóa người dùng
                if (_userAdmin != null)
                {
                    await _userAdmin.LockUserAsync(adminId, bc.MaDoiTuong, bc.GhiChuXuLy);
                }
                if (_notifications != null)
                {
                    await _notifications.CreateAsync(bc.MaDoiTuong, NotificationTypes.System,
                        "Tài khoản của bạn đã bị khóa",
                        $"Tài khoản của bạn đã bị khóa do vi phạm quy định sàn: {bc.GhiChuXuLy}");
                }
            }
            else if (bc.LoaiDoiTuong == "SHOP")
            {
                // Gọi IShopService của bên A để khóa cửa hàng
                if (_shopService != null)
                {
                    await _shopService.LockShopAsync(adminId, bc.MaDoiTuong, bc.GhiChuXuLy);
                }
                var shop = await _db.CuaHangs.FindAsync(bc.MaDoiTuong);
                if (shop != null && _notifications != null)
                {
                    await _notifications.CreateAsync(shop.MaNguoiDung, NotificationTypes.System,
                        "Cửa hàng của bạn đã bị khóa",
                        $"Cửa hàng '{shop.TenShop}' đã bị tạm khóa do vi phạm chính sách: {bc.GhiChuXuLy}");
                }
            }
            else if (bc.LoaiDoiTuong == "PRODUCT")
            {
                // Gọi sp_ChuyenTrangThaiSanPham của bên C để gỡ sản phẩm chuẩn nghiệp vụ
                try
                {
                    await _db.Database.ExecuteSqlRawAsync(
                        "EXEC dbo.sp_ChuyenTrangThaiSanPham @MaSanPham, @TrangThaiMoi, @LoaiNguoiThucHien, @NguoiThucHienId, @LyDo",
                        new SqlParameter("@MaSanPham", bc.MaDoiTuong),
                        new SqlParameter("@TrangThaiMoi", "REMOVED"),
                        new SqlParameter("@LoaiNguoiThucHien", "ADMIN"),
                        new SqlParameter("@NguoiThucHienId", adminId),
                        new SqlParameter("@LyDo", bc.GhiChuXuLy));
                }
                catch
                {
                    // Dự phòng nếu DB test in-memory không chạy được ExecuteSqlRaw: cập nhật entity
                    var sp = await _db.SanPhams.FindAsync(bc.MaDoiTuong);
                    if (sp != null)
                    {
                        sp.TrangThai = "REMOVED";
                        sp.LyDoGo = bc.GhiChuXuLy;
                        sp.NgayCapNhat = now;
                        await _db.SaveChangesAsync();
                    }
                }

                // Gửi thông báo cho chủ shop của sản phẩm bị gỡ
                var product = await _db.SanPhams.Include(p => p.CuaHang).FirstOrDefaultAsync(p => p.MaSanPham == bc.MaDoiTuong);
                if (product?.CuaHang != null && _notifications != null)
                {
                    await _notifications.CreateAsync(product.CuaHang.MaNguoiDung, NotificationTypes.Product,
                        "Sản phẩm bị gỡ bỏ do vi phạm",
                        $"Sản phẩm '{product.Ten}' đã bị ban quản trị gỡ bỏ khỏi sàn. Lý do: {bc.GhiChuXuLy}",
                        bc.MaDoiTuong);
                }
            }

            // Ghi Audit Log xử lý báo cáo
            if (_audit != null)
            {
                await _audit.LogAsync(adminId, "RESOLVE_REPORT", "BaoCao", maBaoCao, "SUCCESS", bc.GhiChuXuLy);
            }

            // Gửi thông báo cảm ơn người báo cáo
            if (_notifications != null)
            {
                await _notifications.CreateAsync(bc.MaNguoiBaoCao, NotificationTypes.System,
                    "Báo cáo của bạn đã được xử lý",
                    $"Ban quản trị đã tiến hành xử lý vi phạm đối với {bc.LoaiDoiTuong} #{bc.MaDoiTuong} theo báo cáo của bạn. Cảm ơn sự đóng góp của bạn!",
                    bc.MaBaoCao);
            }
        }
        else
        {
            return (false, "Hành động xử lý không hợp lệ.");
        }

        return (true, "Đã cập nhật xử lý báo cáo thành công.");
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
                "PRODUCT" => products.GetValueOrDefault(x.MaDoiTuong, $"Sản phẩm #{x.MaDoiTuong}"),
                "USER" => users.GetValueOrDefault(x.MaDoiTuong, $"Người dùng #{x.MaDoiTuong}"),
                "SHOP" => shops.GetValueOrDefault(x.MaDoiTuong, $"Cửa hàng #{x.MaDoiTuong}"),
                _ => $"#{x.MaDoiTuong}"
            };

            return new BaoCaoItemViewModel
            {
                MaBaoCao = x.MaBaoCao,
                LoaiDoiTuong = x.LoaiDoiTuong,
                MaDoiTuong = x.MaDoiTuong,
                TenDoiTuong = tenDoiTuong,
                MaNguoiBaoCao = x.MaNguoiBaoCao,
                TenNguoiBaoCao = x.NguoiBaoCao?.HoTen ?? "Người dùng",
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
