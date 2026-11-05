using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Disputes;
using SecondHandMarketplace.Web.Services.AuditLogs;
using SecondHandMarketplace.Web.Services.Notifications;

namespace SecondHandMarketplace.Web.Services.Disputes;

public class DisputeService : IDisputeService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IAuditLogService _audit;

    public DisputeService(AppDbContext db, INotificationService notifications, IAuditLogService audit)
    {
        _db = db;
        _notifications = notifications;
        _audit = audit;
    }

    public async Task<(bool ThanhCong, string? ThongBao)> MoTranhChapAsync(int maNguoiMua, MoTranhChapViewModel vm)
    {
        // 1. Kiểm tra lý do bắt buộc và độ dài
        if (string.IsNullOrWhiteSpace(vm.LyDo))
        {
            return (false, "Vui lòng nhập lý do mở tranh chấp.");
        }
        var lyDoTrim = vm.LyDo.Trim();
        if (lyDoTrim.Length > 500)
        {
            return (false, "Lý do khiếu nại không được vượt quá 500 ký tự.");
        }

        // 2. Kiểm tra đơn hàng và quyền sở hữu
        var donHang = await _db.DonHangs
            .Include(d => d.CuaHang)
            .FirstOrDefaultAsync(d => d.MaDonHang == vm.MaDonHang);

        if (donHang == null) return (false, "Không tìm thấy đơn hàng.");

        if (donHang.MaNguoiDung != maNguoiMua)
        {
            return (false, "Bạn không có quyền khiếu nại đơn hàng này.");
        }

        // 3. Chỉ cho phép khiếu nại đơn hàng đang giao, đã giao hoặc đã hoàn tất (không cho phép đơn chưa giao hoặc đã hủy)
        if (donHang.TrangThai != "SHIPPING" && donHang.TrangThai != "DELIVERED" && donHang.TrangThai != "COMPLETED")
        {
            return (false, "Chỉ có thể mở khiếu nại đối với đơn hàng đang giao, đã giao hoặc đã hoàn tất.");
        }

        // 4. Chống mở trùng khiếu nại đang xử lý
        var coTranhChapChuaXong = await _db.TranhChaps.AnyAsync(t =>
            t.MaDonHang == vm.MaDonHang && (t.TrangThai == "OPEN" || t.TrangThai == "PROCESSING"));

        if (coTranhChapChuaXong)
        {
            return (false, "Đơn hàng này đang có khiếu nại đang chờ xử lý.");
        }

        var now = DateTime.UtcNow;
        var tranhChap = new TranhChap
        {
            MaDonHang = vm.MaDonHang,
            MaNguoiTao = maNguoiMua,
            LyDo = lyDoTrim,
            BangChungAnh = string.IsNullOrWhiteSpace(vm.BangChungAnh) ? null : vm.BangChungAnh.Trim(),
            TrangThai = "OPEN",
            NgayTao = now,
            NgayCapNhat = now
        };

        _db.TranhChaps.Add(tranhChap);
        await _db.SaveChangesAsync();

        // 5. Gửi thông báo cho chủ shop
        var chuShopId = donHang.CuaHang.MaNguoiDung;
        if (chuShopId > 0)
        {
            await _notifications.CreateAsync(chuShopId, NotificationTypes.Order,
                "Khiếu nại đơn hàng mới",
                $"Người mua đã mở khiếu nại cho đơn hàng #{donHang.MaDon}. Lý do: {lyDoTrim}",
                donHang.MaDonHang);
        }

        return (true, "Đã mở khiếu nại thành công. Người bán và Quản trị viên sẽ sớm phản hồi.");
    }

    public async Task<(bool ThanhCong, string? ThongBao)> NguoiBanPhanHoiAsync(int maChuShop, int maTranhChap, string phanHoi)
    {
        if (string.IsNullOrWhiteSpace(phanHoi))
        {
            return (false, "Nội dung phản hồi không được để trống.");
        }
        var phanHoiTrim = phanHoi.Trim();
        if (phanHoiTrim.Length > 1000)
        {
            return (false, "Nội dung phản hồi không được vượt quá 1000 ký tự.");
        }

        var tc = await _db.TranhChaps
            .Include(t => t.DonHang)
            .ThenInclude(d => d.CuaHang)
            .FirstOrDefaultAsync(t => t.MaTranhChap == maTranhChap);

        if (tc == null) return (false, "Không tìm thấy tranh chấp.");

        if (tc.DonHang.CuaHang.MaNguoiDung != maChuShop)
        {
            return (false, "Bạn không phải chủ shop của đơn hàng này.");
        }

        if (tc.TrangThai != "OPEN" && tc.TrangThai != "PROCESSING")
        {
            return (false, "Tranh chấp này đã kết thúc, không thể gửi thêm phản hồi.");
        }

        tc.PhanHoiNguoiBan = phanHoiTrim;
        tc.TrangThai = "PROCESSING";
        tc.NgayCapNhat = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Gửi thông báo cho người mua
        await _notifications.CreateAsync(tc.MaNguoiTao, NotificationTypes.Order,
            "Người bán đã phản hồi khiếu nại",
            $"Chủ cửa hàng {tc.DonHang.CuaHang.TenShop} đã phản hồi khiếu nại đơn hàng #{tc.DonHang.MaDon}: {phanHoiTrim}",
            tc.MaDonHang);

        return (true, "Đã gửi phản hồi tranh chấp thành công.");
    }

    public async Task<TranhChapDetailViewModel?> LayChiTietTranhChapAsync(int maTranhChap)
    {
        var tc = await _db.TranhChaps
            .Include(t => t.DonHang)
                .ThenInclude(d => d.CuaHang)
            .Include(t => t.NguoiTao)
            .Include(t => t.NguoiXuLy)
            .FirstOrDefaultAsync(t => t.MaTranhChap == maTranhChap);

        if (tc == null) return null;

        return new TranhChapDetailViewModel
        {
            MaTranhChap = tc.MaTranhChap,
            MaDonHang = tc.MaDonHang,
            MaDon = tc.DonHang.MaDon,
            MaNguoiTao = tc.MaNguoiTao,
            TenNguoiTao = tc.NguoiTao.HoTen,
            MaChuShop = tc.DonHang.CuaHang.MaNguoiDung,
            TenShop = tc.DonHang.CuaHang.TenShop,
            TongTien = tc.DonHang.TongTien,
            TrangThaiDonHang = tc.DonHang.TrangThai,
            LyDo = tc.LyDo,
            BangChungAnh = tc.BangChungAnh,
            PhanHoiNguoiBan = tc.PhanHoiNguoiBan,
            TrangThai = tc.TrangThai,
            PhanQuyet = tc.PhanQuyet,
            GhiChuAdmin = tc.GhiChuAdmin,
            NguoiXuLyId = tc.NguoiXuLyId,
            TenNguoiXuLy = tc.NguoiXuLy?.HoTen,
            NgayTao = tc.NgayTao,
            NgayCapNhat = tc.NgayCapNhat,
            NgayGiaiQuyet = tc.NgayGiaiQuyet
        };
    }

    public async Task<List<TranhChapDetailViewModel>> LayDanhSachTranhChapNguoiDungAsync(int maNguoiDung)
    {
        var list = await _db.TranhChaps
            .Include(t => t.DonHang)
                .ThenInclude(d => d.CuaHang)
            .Include(t => t.NguoiTao)
            .Include(t => t.NguoiXuLy)
            .Where(t => t.MaNguoiTao == maNguoiDung || t.DonHang.CuaHang.MaNguoiDung == maNguoiDung)
            .OrderByDescending(t => t.NgayTao)
            .ToListAsync();

        return list.Select(tc => new TranhChapDetailViewModel
        {
            MaTranhChap = tc.MaTranhChap,
            MaDonHang = tc.MaDonHang,
            MaDon = tc.DonHang.MaDon,
            MaNguoiTao = tc.MaNguoiTao,
            TenNguoiTao = tc.NguoiTao.HoTen,
            MaChuShop = tc.DonHang.CuaHang.MaNguoiDung,
            TenShop = tc.DonHang.CuaHang.TenShop,
            TongTien = tc.DonHang.TongTien,
            TrangThaiDonHang = tc.DonHang.TrangThai,
            LyDo = tc.LyDo,
            BangChungAnh = tc.BangChungAnh,
            PhanHoiNguoiBan = tc.PhanHoiNguoiBan,
            TrangThai = tc.TrangThai,
            PhanQuyet = tc.PhanQuyet,
            GhiChuAdmin = tc.GhiChuAdmin,
            NguoiXuLyId = tc.NguoiXuLyId,
            TenNguoiXuLy = tc.NguoiXuLy?.HoTen,
            NgayTao = tc.NgayTao,
            NgayCapNhat = tc.NgayCapNhat,
            NgayGiaiQuyet = tc.NgayGiaiQuyet
        }).ToList();
    }

    public async Task<AdminDisputeListViewModel> LayDanhSachTranhChapAdminAsync(string? trangThai)
    {
        var query = _db.TranhChaps
            .Include(t => t.DonHang)
                .ThenInclude(d => d.CuaHang)
            .Include(t => t.NguoiTao)
            .Include(t => t.NguoiXuLy)
            .AsQueryable();

        if (!string.IsNullOrEmpty(trangThai))
        {
            query = query.Where(t => t.TrangThai == trangThai);
        }

        var list = await query.OrderByDescending(t => t.NgayTao).ToListAsync();
        var items = list.Select(tc => new TranhChapDetailViewModel
        {
            MaTranhChap = tc.MaTranhChap,
            MaDonHang = tc.MaDonHang,
            MaDon = tc.DonHang.MaDon,
            MaNguoiTao = tc.MaNguoiTao,
            TenNguoiTao = tc.NguoiTao.HoTen,
            MaChuShop = tc.DonHang.CuaHang.MaNguoiDung,
            TenShop = tc.DonHang.CuaHang.TenShop,
            TongTien = tc.DonHang.TongTien,
            TrangThaiDonHang = tc.DonHang.TrangThai,
            LyDo = tc.LyDo,
            BangChungAnh = tc.BangChungAnh,
            PhanHoiNguoiBan = tc.PhanHoiNguoiBan,
            TrangThai = tc.TrangThai,
            PhanQuyet = tc.PhanQuyet,
            GhiChuAdmin = tc.GhiChuAdmin,
            NguoiXuLyId = tc.NguoiXuLyId,
            TenNguoiXuLy = tc.NguoiXuLy?.HoTen,
            NgayTao = tc.NgayTao,
            NgayCapNhat = tc.NgayCapNhat,
            NgayGiaiQuyet = tc.NgayGiaiQuyet
        }).ToList();

        var tongChuaGiaiQuyet = await _db.TranhChaps.CountAsync(t => t.TrangThai == "OPEN" || t.TrangThai == "PROCESSING");

        return new AdminDisputeListViewModel
        {
            TrangThaiFilter = trangThai,
            Items = items,
            TongChuaGiaiQuyet = tongChuaGiaiQuyet
        };
    }

    public async Task<(bool ThanhCong, string? ThongBao)> XuLyTranhChapAdminAsync(int maTranhChap, int adminId, string phanQuyet, string? ghiChu)
    {
        var tc = await _db.TranhChaps
            .Include(t => t.DonHang)
                .ThenInclude(d => d.CuaHang)
            .FirstOrDefaultAsync(t => t.MaTranhChap == maTranhChap);

        if (tc == null) return (false, "Không tìm thấy tranh chấp.");
        if (tc.TrangThai == "RESOLVED" || tc.TrangThai == "REJECTED")
        {
            return (false, "Tranh chấp này đã được phân xử trước đó.");
        }

        var now = DateTime.UtcNow;
        tc.NguoiXuLyId = adminId;
        tc.NgayGiaiQuyet = now;
        tc.GhiChuAdmin = ghiChu;
        var trangThaiDonCu = tc.DonHang.TrangThai;

        if (phanQuyet == "REFUND_BUYER")
        {
            tc.TrangThai = "RESOLVED";
            tc.PhanQuyet = "Chấp thuận khiếu nại: Hoàn tiền cho người mua và hủy đơn.";
            tc.DonHang.TrangThai = "CANCELLED";
            tc.DonHang.TrangThaiThanhToan = "REFUNDED";
            tc.DonHang.NgayCapNhat = now;
            tc.DonHang.NguoiHuy = "ADMIN";
            tc.DonHang.LyDoHuy = string.IsNullOrWhiteSpace(ghiChu) ? "Admin xử lý tranh chấp: Hoàn tiền cho người mua." : ghiChu.Trim();
            tc.DonHang.ThoiGianHuy = now;

            // Nhả hàng giữ chỗ
            var chiTiets = await _db.ChiTietDonHangs
                .Include(c => c.SanPham)
                .Where(c => c.MaDonHang == tc.MaDonHang).ToListAsync();

            foreach (var ct in chiTiets)
            {
                ct.DangGiuCho = false;
                if (ct.SanPham != null && ct.SanPham.TrangThai == "RESERVED")
                {
                    ct.SanPham.TrangThai = "ACTIVE";
                    ct.SanPham.NgayCapNhat = now;
                }
            }

            _db.LichSuTrangThaiDonHangs.Add(new LichSuTrangThaiDonHang
            {
                MaDonHang = tc.MaDonHang,
                TrangThaiCu = trangThaiDonCu,
                TrangThaiMoi = "CANCELLED",
                LoaiNguoiThucHien = "ADMIN",
                NguoiThucHienId = adminId,
                LyDo = tc.PhanQuyet,
                ThoiGian = now
            });
        }
        else if (phanQuyet == "PAY_SELLER" || phanQuyet == "RELEASE_SELLER")
        {
            tc.TrangThai = "RESOLVED";
            tc.PhanQuyet = "Bác bỏ khiếu nại: Thanh toán hoàn tất cho người bán.";
            tc.DonHang.TrangThai = "COMPLETED";
            tc.DonHang.TrangThaiThanhToan = "PAID";
            tc.DonHang.NgayCapNhat = now;
            tc.DonHang.ThoiGianHoanTat = now;

            var chiTiets = await _db.ChiTietDonHangs
                .Include(c => c.SanPham)
                .Where(c => c.MaDonHang == tc.MaDonHang).ToListAsync();

            foreach (var ct in chiTiets)
            {
                ct.DangGiuCho = false;
                if (ct.SanPham != null)
                {
                    ct.SanPham.TrangThai = "SOLD";
                    ct.SanPham.NgayCapNhat = now;
                }
            }

            _db.LichSuTrangThaiDonHangs.Add(new LichSuTrangThaiDonHang
            {
                MaDonHang = tc.MaDonHang,
                TrangThaiCu = trangThaiDonCu,
                TrangThaiMoi = "COMPLETED",
                LoaiNguoiThucHien = "ADMIN",
                NguoiThucHienId = adminId,
                LyDo = tc.PhanQuyet,
                ThoiGian = now
            });
        }
        else if (phanQuyet == "REJECT")
        {
            tc.TrangThai = "REJECTED";
            tc.PhanQuyet = "Bác bỏ khiếu nại tranh chấp.";
            // DonHang.TrangThai giữ nguyên
        }
        else
        {
            return (false, "Quyết định xử lý không hợp lệ.");
        }

        await _db.SaveChangesAsync();

        // Ghi Audit Log
        await _audit.LogAsync(adminId, "RESOLVE_DISPUTE", "TranhChap", tc.MaTranhChap, "SUCCESS",
            $"Phán quyết: {phanQuyet}. Ghi chú: {ghiChu}");

        // Gửi thông báo cho cả 2 bên
        await _notifications.CreateAsync(tc.MaNguoiTao, NotificationTypes.Order,
            "Kết quả xử lý khiếu nại đơn hàng",
            $"Ban quản trị đã giải quyết khiếu nại cho đơn hàng #{tc.DonHang.MaDon}: {tc.PhanQuyet}",
            tc.MaDonHang);

        var chuShopId = tc.DonHang.CuaHang.MaNguoiDung;
        if (chuShopId > 0)
        {
            await _notifications.CreateAsync(chuShopId, NotificationTypes.Order,
                "Kết quả xử lý khiếu nại đơn hàng",
                $"Ban quản trị đã giải quyết khiếu nại cho đơn hàng #{tc.DonHang.MaDon}: {tc.PhanQuyet}",
                tc.MaDonHang);
        }

        return (true, "Đã lưu phán quyết xử lý tranh chấp thành công.");
    }
}
