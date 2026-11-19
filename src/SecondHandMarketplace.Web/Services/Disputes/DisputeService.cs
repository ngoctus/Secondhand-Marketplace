using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
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

    public async Task<(bool HopLe, string? ThongBao, MoTranhChapViewModel? ViewModel, int? MaTranhChapHienCo)> ChuanBiMoTranhChapAsync(int userId, int maDonHang)
    {
        var donHang = await _db.DonHangs
            .Include(d => d.CuaHang)
            .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang && d.MaNguoiDung == userId);

        if (donHang == null)
        {
            return (false, "Không tìm thấy đơn hàng cần khiếu nại hoặc bạn không phải người mua.", null, null);
        }

        if (donHang.TrangThai != "SHIPPING" && donHang.TrangThai != "DELIVERED" && donHang.TrangThai != "COMPLETED")
        {
            return (false, "Chỉ có thể mở khiếu nại đối với đơn hàng đang giao, đã giao hoặc đã hoàn tất.", null, null);
        }

        var tranhChapHienCo = await _db.TranhChaps.FirstOrDefaultAsync(t => t.MaDonHang == maDonHang);
        if (tranhChapHienCo != null)
        {
            return (false, "Đơn hàng này đã có khiếu nại trước đó.", null, tranhChapHienCo.MaTranhChap);
        }

        var vm = new MoTranhChapViewModel
        {
            MaDonHang = donHang.MaDonHang,
            MaDon = donHang.MaDon,
            TenShop = donHang.CuaHang?.TenShop ?? "Cửa hàng",
            TongTien = donHang.TongTien
        };

        return (true, null, vm, null);
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

        // Kiểm tra độ dài đường dẫn bằng chứng ảnh
        if (!string.IsNullOrWhiteSpace(vm.BangChungAnh) && vm.BangChungAnh.Trim().Length > 500)
        {
            return (false, "Đường dẫn ảnh bằng chứng không được vượt quá 500 ký tự.");
        }

        if (!string.IsNullOrWhiteSpace(vm.BangChungAnh) && !UrlAnToan.HopLe(vm.BangChungAnh))
        {
            return (false, "Đường dẫn ảnh bằng chứng phải là URL http/https.");
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

        // 4. Chống mở trùng khiếu nại: Mỗi đơn hàng chỉ được mở khiếu nại 1 lần duy nhất
        var daCoTranhChap = await _db.TranhChaps.AnyAsync(t => t.MaDonHang == vm.MaDonHang);

        if (daCoTranhChap)
        {
            return (false, "Đơn hàng này đã có khiếu nại đang chờ xử lý hoặc đã kết thúc và không thể mở lại.");
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
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (false, "Đơn hàng này đang có khiếu nại đang chờ xử lý.");
        }

        // Gửi thông báo cho Người bán (chủ cửa hàng) - best effort
        try
        {
            await _notifications.CreateAsync(
                donHang.CuaHang.MaNguoiDung,
                NotificationTypes.Order,
                "Đơn hàng phát sinh khiếu nại tranh chấp",
                $"Người mua đã mở khiếu nại cho đơn hàng #{donHang.MaDon}. Lý do: {lyDoTrim}. Vui lòng kiểm tra và phản hồi.",
                donHang.MaDonHang
            );
        }
        catch { /* Best effort: không làm gián đoạn khiếu nại đã lưu */ }

        return (true, "Đã gửi yêu cầu khiếu nại tranh chấp thành công. Người bán và Ban quản trị sẽ tiến hành xử lý.");
    }

    public async Task<(bool ThanhCong, string? ThongBao)> NguoiBanPhanHoiAsync(int maChuShop, int maTranhChap, string phanHoi)
    {
        if (string.IsNullOrWhiteSpace(phanHoi))
        {
            return (false, "Vui lòng nhập nội dung phản hồi.");
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

        // Kiểm tra quyền: Người phản hồi phải là chủ cửa hàng của đơn hàng này
        if (tc.DonHang.CuaHang.MaNguoiDung != maChuShop)
        {
            return (false, "Bạn không có quyền phản hồi tranh chấp của đơn hàng này.");
        }

        if (tc.TrangThai != "OPEN" && tc.TrangThai != "PROCESSING")
        {
            return (false, "Tranh chấp này đã được đóng hoặc giải quyết trước đó.");
        }

        tc.PhanHoiNguoiBan = phanHoiTrim;
        tc.NgayCapNhat = DateTime.UtcNow;
        tc.TrangThai = "PROCESSING";

        await _db.SaveChangesAsync();

        // Gửi thông báo cho Người mua (best-effort)
        try
        {
            await _notifications.CreateAsync(
                tc.MaNguoiTao,
                NotificationTypes.Order,
                "Người bán đã phản hồi khiếu nại",
                $"Người bán đã phản hồi khiếu nại đơn hàng #{tc.DonHang.MaDon}: \"{phanHoiTrim}\"",
                tc.MaDonHang
            );
        }
        catch { /* Best-effort: không làm hỏng phản hồi nếu thông báo thất bại */ }

        return (true, "Đã gửi phản hồi thành công.");
    }

    public async Task<TranhChapDetailViewModel?> LayChiTietTranhChapAsync(int maTranhChap)
    {
        var tc = await _db.TranhChaps
            .Include(t => t.DonHang)
                .ThenInclude(d => d.CuaHang)
            .Include(t => t.DonHang)
                .ThenInclude(d => d.NguoiDung)
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
            MaNguoiMua = tc.DonHang.MaNguoiDung,
            TenNguoiMua = tc.DonHang.NguoiDung?.HoTen ?? tc.DonHang.TenNguoiNhan,
            MaChuShop = tc.DonHang.CuaHang.MaNguoiDung,
            TenShop = tc.DonHang.CuaHang.TenShop,
            TongTien = tc.DonHang.TongTien,
            TrangThaiDonHang = tc.DonHang.TrangThai,
            LyDo = tc.LyDo,
            BangChungAnh = tc.BangChungAnh,
            PhanHoiNguoiBan = tc.PhanHoiNguoiBan,
            NgayPhanHoi = tc.PhanHoiNguoiBan != null ? tc.NgayCapNhat : null,
            TrangThai = tc.TrangThai,
            PhanQuyet = tc.PhanQuyet,
            NguoiXuLyId = tc.NguoiXuLyId,
            TenNguoiXuLy = tc.NguoiXuLy?.HoTen,
            GhiChuAdmin = tc.GhiChuAdmin,
            NgayTao = tc.NgayTao,
            NgayCapNhat = tc.NgayCapNhat,
            NgayGiaiQuyet = tc.NgayGiaiQuyet
        };
    }

    public async Task<List<TranhChapDetailViewModel>> LayDanhSachTranhChapNguoiDungAsync(int maNguoiDung)
    {
        // Lấy tất cả tranh chấp mà người dùng là người mua hoặc là người bán (chủ shop)
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
            MaNguoiMua = tc.DonHang.MaNguoiDung,
            TenNguoiMua = tc.DonHang.TenNguoiNhan,
            MaChuShop = tc.DonHang.CuaHang.MaNguoiDung,
            TenShop = tc.DonHang.CuaHang.TenShop,
            TongTien = tc.DonHang.TongTien,
            TrangThaiDonHang = tc.DonHang.TrangThai,
            LyDo = tc.LyDo,
            BangChungAnh = tc.BangChungAnh,
            PhanHoiNguoiBan = tc.PhanHoiNguoiBan,
            NgayPhanHoi = tc.PhanHoiNguoiBan != null ? tc.NgayCapNhat : null,
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

    public async Task<AdminDisputeListViewModel> LayDanhSachTranhChapAdminAsync(string? trangThai, int page = 1, int pageSize = 10)
    {
        page = Math.Clamp(page, 1, 10000);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.TranhChaps
            .Include(t => t.DonHang)
                .ThenInclude(d => d.CuaHang)
            .Include(t => t.NguoiTao)
            .Include(t => t.NguoiXuLy)
            .AsQueryable();

        var ttChuan = trangThai?.Trim().ToUpperInvariant();
        if (ttChuan is "OPEN" or "PROCESSING" or "RESOLVED" or "REJECTED")
        {
            query = query.Where(t => t.TrangThai == ttChuan);
        }

        var totalItems = await query.CountAsync();
        var list = await query
            .OrderByDescending(t => t.NgayTao)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = list.Select(tc => new TranhChapDetailViewModel
        {
            MaTranhChap = tc.MaTranhChap,
            MaDonHang = tc.MaDonHang,
            MaDon = tc.DonHang.MaDon,
            MaNguoiTao = tc.MaNguoiTao,
            TenNguoiTao = tc.NguoiTao.HoTen,
            MaNguoiMua = tc.DonHang.MaNguoiDung,
            TenNguoiMua = tc.DonHang.TenNguoiNhan,
            MaChuShop = tc.DonHang.CuaHang.MaNguoiDung,
            TenShop = tc.DonHang.CuaHang.TenShop,
            TongTien = tc.DonHang.TongTien,
            TrangThaiDonHang = tc.DonHang.TrangThai,
            LyDo = tc.LyDo,
            BangChungAnh = tc.BangChungAnh,
            PhanHoiNguoiBan = tc.PhanHoiNguoiBan,
            NgayPhanHoi = tc.PhanHoiNguoiBan != null ? tc.NgayCapNhat : null,
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
            TongChuaGiaiQuyet = tongChuaGiaiQuyet,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
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

        if (tc.DonHang.TrangThai == "CANCELLED" && phanQuyet == "REFUND_BUYER")
        {
            return (false, "Đơn hàng đã ở trạng thái Đã hủy, không thể chọn phán quyết hoàn tiền. Vui lòng chọn Bác bỏ khiếu nại.");
        }

        if (phanQuyet == "RELEASE_SELLER" && (tc.DonHang.TrangThai == "CANCELLED" || tc.DonHang.TrangThai == "COMPLETED"))
        {
            return (false, "Trạng thái đơn hàng không phù hợp với phán quyết thanh toán cho người bán.");
        }

        if (!string.IsNullOrWhiteSpace(ghiChu) && ghiChu.Trim().Length > 500)
        {
            return (false, "Ghi chú của quản trị viên không được vượt quá 500 ký tự.");
        }

        using var tx = _db.Database.IsRelational() ? await _db.Database.BeginTransactionAsync() : null;
        var now = DateTime.UtcNow;
        tc.NguoiXuLyId = adminId;
        tc.NgayGiaiQuyet = now;
        tc.GhiChuAdmin = ghiChu?.Trim();
        var trangThaiDonCu = tc.DonHang.TrangThai;

        try
        {
        if (phanQuyet == "REFUND_BUYER")
        {
            tc.TrangThai = "RESOLVED";
            tc.PhanQuyet = "Chấp thuận khiếu nại: Hoàn tiền cho người mua và hủy đơn.";
            var lyDoAdmin = string.IsNullOrWhiteSpace(ghiChu) ? tc.PhanQuyet : $"{tc.PhanQuyet} Ghi chú: {ghiChu.Trim()}";
            if (lyDoAdmin.Length > 500)
            {
                var cut = 500;
                if (char.IsHighSurrogate(lyDoAdmin[cut - 1])) cut--;
                lyDoAdmin = lyDoAdmin[..cut];
            }

            if (_db.Database.IsRelational())
            {
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC dbo.sp_AdminCapNhatTrangThaiDon @MaDonHang={tc.MaDonHang}, @TrangThaiMoi={"CANCELLED"}, @QuanTriVienId={adminId}, @LyDo={lyDoAdmin}");
            }
            else
            {
                tc.DonHang.TrangThai = "CANCELLED";
                tc.DonHang.TrangThaiThanhToan = "REFUNDED";
                tc.DonHang.NgayCapNhat = now;
                tc.DonHang.NguoiHuy = "ADMIN";
                tc.DonHang.LyDoHuy = lyDoAdmin;
                tc.DonHang.ThoiGianHuy = now;

                var tt = await _db.ThanhToans.FirstOrDefaultAsync(t => t.MaDonHang == tc.MaDonHang);
                if (tt != null)
                {
                    tt.TrangThaiThanhToan = "REFUNDED";
                }

                // Hoàn lại sản phẩm về ACTIVE bất kể đang RESERVED hay đã SOLD (khi đơn COMPLETED bị refund)
                var chiTiets = await _db.ChiTietDonHangs
                    .Include(c => c.SanPham)
                    .Where(c => c.MaDonHang == tc.MaDonHang).ToListAsync();

                foreach (var ct in chiTiets)
                {
                    ct.DangGiuCho = false;
                    if (ct.SanPham != null && (ct.SanPham.TrangThai == "RESERVED" || ct.SanPham.TrangThai == "SOLD"))
                    {
                        var spCu = ct.SanPham.TrangThai;
                        ct.SanPham.TrangThai = "ACTIVE";
                        ct.SanPham.NgayCapNhat = now;

                        _db.LichSuTrangThaiSanPhams.Add(new LichSuTrangThaiSanPham
                        {
                            MaSanPham = ct.MaSanPham,
                            TrangThaiCu = spCu,
                            TrangThaiMoi = "ACTIVE",
                            LoaiNguoiThucHien = "ADMIN",
                            NguoiThucHienId = adminId,
                            LyDo = $"Admin hoàn tiền đơn #{tc.DonHang.MaDon}: {tc.PhanQuyet}",
                            ThoiGian = now
                        });
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
        }
        else if (phanQuyet == "PAY_SELLER" || phanQuyet == "RELEASE_SELLER")
        {
            tc.TrangThai = "RESOLVED";
            tc.PhanQuyet = "Bác bỏ khiếu nại: Thanh toán hoàn tất cho người bán.";
            var lyDoAdmin = string.IsNullOrWhiteSpace(ghiChu) ? tc.PhanQuyet : $"{tc.PhanQuyet} Ghi chú: {ghiChu.Trim()}";
            if (lyDoAdmin.Length > 500)
            {
                var cut = 500;
                if (char.IsHighSurrogate(lyDoAdmin[cut - 1])) cut--;
                lyDoAdmin = lyDoAdmin[..cut];
            }

            // Fix #6: Nếu đơn đã COMPLETED thì không gọi đổi trạng thái đơn nữa
            if (tc.DonHang.TrangThai != "COMPLETED")
            {
                if (_db.Database.IsRelational())
                {
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"EXEC dbo.sp_AdminCapNhatTrangThaiDon @MaDonHang={tc.MaDonHang}, @TrangThaiMoi={"COMPLETED"}, @QuanTriVienId={adminId}, @LyDo={lyDoAdmin}");
                }
                else
                {
                    tc.DonHang.TrangThai = "COMPLETED";
                    tc.DonHang.TrangThaiThanhToan = "PAID";
                    tc.DonHang.NgayCapNhat = now;
                    tc.DonHang.ThoiGianHoanTat = now;

                    var tt = await _db.ThanhToans.FirstOrDefaultAsync(t => t.MaDonHang == tc.MaDonHang);
                    if (tt != null)
                    {
                        tt.TrangThaiThanhToan = "SUCCESS";
                        tt.ThoiGianThanhToan = now;
                                            }

                    var chiTiets = await _db.ChiTietDonHangs
                        .Include(c => c.SanPham)
                        .Where(c => c.MaDonHang == tc.MaDonHang).ToListAsync();

                    foreach (var ct in chiTiets)
                    {
                        ct.DangGiuCho = false;
                        if (ct.SanPham != null && ct.SanPham.TrangThai == "RESERVED")
                        {
                            var spCu = ct.SanPham.TrangThai;
                            ct.SanPham.TrangThai = "SOLD";
                            ct.SanPham.NgayCapNhat = now;

                            _db.LichSuTrangThaiSanPhams.Add(new LichSuTrangThaiSanPham
                            {
                                MaSanPham = ct.MaSanPham,
                                TrangThaiCu = spCu,
                                TrangThaiMoi = "SOLD",
                                LoaiNguoiThucHien = "ADMIN",
                                NguoiThucHienId = adminId,
                                LyDo = $"Admin hoàn tất đơn #{tc.DonHang.MaDon}: {tc.PhanQuyet}",
                                ThoiGian = now
                            });
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
            }
        }
        else if (phanQuyet == "REJECT")
        {
            tc.TrangThai = "REJECTED";
            tc.PhanQuyet = string.IsNullOrWhiteSpace(ghiChu) ? "Bác bỏ khiếu nại tranh chấp." : $"Bác bỏ khiếu nại: {ghiChu.Trim()}";
            // DonHang.TrangThai giữ nguyên theo quy tắc
        }
        else
        {
            return (false, "Quyết định xử lý không hợp lệ. Vui lòng chọn Hoàn tiền cho người mua, Thanh toán cho người bán hoặc Bác bỏ khiếu nại.");
        }

        await _db.SaveChangesAsync();
        if (tx != null) await tx.CommitAsync();
    }
    catch (Microsoft.Data.SqlClient.SqlException sqlEx)
    {
        if (tx != null)
        {
            try { await tx.RollbackAsync(); } catch { }
        }
        return (false, SecondHandMarketplace.Web.Common.SqlLoi.ThongBao(sqlEx));
    }
    catch (Exception ex)
    {
        if (tx != null)
        {
            try { await tx.RollbackAsync(); } catch { }
        }
        return (false, SecondHandMarketplace.Web.Common.SqlLoi.ThongBao(ex));
    }

    // Audit log và thông báo thực hiện sau khi transaction đã commit thành công.
    // Nếu chúng thất bại không làm rollback lại đơn hàng đã xử lý xong.
    try
    {
        await _audit.LogAsync(adminId, "RESOLVE_DISPUTE", "TranhChap", tc.MaTranhChap, "SUCCESS",
            $"Phán quyết: {phanQuyet}. Ghi chú: {ghiChu}");

        await _notifications.CreateAsync(tc.MaNguoiTao, NotificationTypes.Order,
            "Kết quả xử lý khiếu nại đơn hàng",
            $"Tranh chấp đơn hàng #{tc.DonHang.MaDon} đã có kết quả: {tc.PhanQuyet}",
            tc.MaDonHang);

        await _notifications.CreateAsync(tc.DonHang.CuaHang.MaNguoiDung, NotificationTypes.Order,
            "Kết quả xử lý khiếu nại đơn hàng",
            $"Tranh chấp đơn hàng #{tc.DonHang.MaDon} đã có kết quả: {tc.PhanQuyet}",
            tc.MaDonHang);
    }
    catch
    {
        // Best effort: không làm thay đổi kết quả đã lưu vào DB.
    }

    return (true, "Đã lưu phán quyết tranh chấp và cập nhật đơn hàng thành công.");
}
}
