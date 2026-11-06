using SecondHandMarketplace.Web.Data;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Reviews;

namespace SecondHandMarketplace.Web.Services.Reviews;

public class ReviewService : IReviewService
{
    private readonly AppDbContext _db;

    public ReviewService(AppDbContext db) => _db = db;

    public async Task<UserReviewListViewModel> LayDanhSachChoDanhGiaAsync(int maNguoiMua)
    {
        var reviewedItems = _db.DanhGias.Select(x => x.MaChiTietDonHang);
        var items = await _db.ChiTietDonHangs
            .AsNoTracking()
            .Include(x => x.DonHang).ThenInclude(x => x.CuaHang)
            .Where(x => x.DonHang.MaNguoiDung == maNguoiMua
                && x.DonHang.TrangThai == "COMPLETED"
                && !reviewedItems.Contains(x.MaChiTietDonHang))
            .OrderByDescending(x => x.DonHang.ThoiGianHoanTat)
            .Select(x => new DanhGiaChoPhepViewModel
            {
                MaChiTietDonHang = x.MaChiTietDonHang,
                MaDonHang = x.MaDonHang,
                MaDon = x.DonHang.MaDon,
                TenSanPham = x.TenSanPham,
                TenShop = x.DonHang.CuaHang.TenShop,
                NgayHoanTat = x.DonHang.ThoiGianHoanTat ?? x.DonHang.NgayCapNhat
            })
            .ToListAsync();
        return new UserReviewListViewModel { ChoDanhGia = items };
    }

    public async Task<(bool ThanhCong, string ThongBao)> TaoDanhGiaAsync(int maNguoiMua, TaoDanhGiaViewModel model)
    {
        var item = await _db.ChiTietDonHangs
            .Include(x => x.DonHang)
            .FirstOrDefaultAsync(x => x.MaChiTietDonHang == model.MaChiTietDonHang
                && x.DonHang.MaNguoiDung == maNguoiMua
                && x.DonHang.TrangThai == "COMPLETED");
        if (item == null) return (false, "Đơn hàng không đủ điều kiện để đánh giá.");
        if (await _db.DanhGias.AnyAsync(x => x.MaChiTietDonHang == item.MaChiTietDonHang))
            return (false, "Sản phẩm này đã được đánh giá.");

        var shopId = item.DonHang.MaCuaHang;
        _db.DanhGias.Add(new DanhGia
        {
            MaDonHang = item.MaDonHang,
            MaChiTietDonHang = item.MaChiTietDonHang,
            MaNguoiMua = maNguoiMua,
            MaCuaHang = shopId,
            SoSao = model.SoSao,
            NoiDung = string.IsNullOrWhiteSpace(model.NoiDung) ? null : model.NoiDung.Trim(),
            TrangThai = "PENDING"
        });
        await _db.SaveChangesAsync();
        return (true, "Đã gửi đánh giá, chờ quản trị viên duyệt.");
    }

    public async Task<AdminReviewListViewModel> LayDanhSachAdminAsync(int page)
    {
        page = Math.Max(1, page);
        const int pageSize = 20;
        var query = _db.DanhGias.AsNoTracking().Include(x => x.DonHang).Include(x => x.NguoiMua).Include(x => x.CuaHang);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.NgayTao).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AdminReviewItemViewModel
            {
                MaDanhGia = x.MaDanhGia, MaDon = x.DonHang.MaDon, TenSanPham = x.ChiTietDonHang.TenSanPham,
                TenNguoiMua = x.NguoiMua.HoTen, TenShop = x.CuaHang.TenShop, SoSao = x.SoSao,
                NoiDung = x.NoiDung, TrangThai = x.TrangThai, NgayTao = x.NgayTao, GhiChuAdmin = x.GhiChuAdmin
            }).ToListAsync();
        return new AdminReviewListViewModel { Items = items, Page = page, PageSize = pageSize, TotalItems = total };
    }

    public async Task<(bool ThanhCong, string ThongBao)> DuyetAsync(int maDanhGia, int adminId, string trangThai, string? ghiChu)
    {
        if (trangThai is not ("APPROVED" or "REJECTED")) return (false, "Trạng thái xử lý không hợp lệ.");
        var review = await _db.DanhGias.FirstOrDefaultAsync(x => x.MaDanhGia == maDanhGia);
        if (review == null) return (false, "Không tìm thấy đánh giá.");
        review.TrangThai = trangThai;
        review.MaAdminXuLy = adminId;
        review.GhiChuAdmin = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim();
        review.NgayDuyet = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, trangThai == "APPROVED" ? "Đã duyệt đánh giá." : "Đã từ chối đánh giá.");
    }
}
