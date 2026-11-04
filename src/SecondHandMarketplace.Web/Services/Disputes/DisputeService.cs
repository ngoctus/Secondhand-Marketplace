using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Disputes;

namespace SecondHandMarketplace.Web.Services.Disputes;

public class DisputeService : IDisputeService
{
    private readonly AppDbContext _db;

    public DisputeService(AppDbContext db) => _db = db;

    public async Task<(bool ThanhCong, string? ThongBao)> MoTranhChapAsync(int maNguoiMua, MoTranhChapViewModel vm)
    {
        var donHang = await _db.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == vm.MaDonHang);
        if (donHang == null) return (false, "Không tìm thấy đơn hàng.");

        if (donHang.MaNguoiDung != maNguoiMua)
        {
            return (false, "Bạn không có quyền khiếu nại đơn hàng này.");
        }

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
            LyDo = vm.LyDo.Trim(),
            BangChungAnh = vm.BangChungAnh,
            TrangThai = "OPEN",
            NgayTao = now,
            NgayCapNhat = now
        };

        _db.TranhChaps.Add(tranhChap);
        await _db.SaveChangesAsync();

        return (true, "Đã mở khiếu nại thành công. Người bán và Quản trị viên sẽ sớm phản hồi.");
    }

    public async Task<(bool ThanhCong, string? ThongBao)> NguoiBanPhanHoiAsync(int maChuShop, int maTranhChap, string phanHoi)
    {
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

        tc.PhanHoiNguoiBan = phanHoi.Trim();
        tc.TrangThai = "PROCESSING";
        tc.NgayCapNhat = DateTime.UtcNow;

        await _db.SaveChangesAsync();
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

        if (phanQuyet == "REFUND_BUYER")
        {
            tc.TrangThai = "RESOLVED";
            tc.PhanQuyet = "Chấp thuận khiếu nại: Hoàn tiền cho người mua.";
            tc.DonHang.TrangThaiThanhToan = "REFUNDED";
            tc.DonHang.NgayCapNhat = now;
        }
        else if (phanQuyet == "PAY_SELLER")
        {
            tc.TrangThai = "RESOLVED";
            tc.PhanQuyet = "Bác bỏ khiếu nại: Thanh toán hoàn tất cho người bán.";
        }
        else if (phanQuyet == "REJECT")
        {
            tc.TrangThai = "REJECTED";
            tc.PhanQuyet = "Bác bỏ khiếu nại tranh chấp.";
        }
        else
        {
            return (false, "Quyết định xử lý không hợp lệ.");
        }

        await _db.SaveChangesAsync();
        return (true, "Đã lưu phán quyết xử lý tranh chấp thành công.");
    }
}
