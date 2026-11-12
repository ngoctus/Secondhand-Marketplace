using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.ViewModels.Reviews;
using SecondHandMarketplace.Web.Services.Notifications;
using SecondHandMarketplace.Web.Services.Reviews;
using SecondHandMarketplace.Web.Services.Shops;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

/// <summary>
/// C4: Admin rà soát shop bị đánh giá thấp.
/// Luồng chính: xem danh sách shop dưới 3 sao → xem chi tiết đánh giá → ẩn đánh giá vi phạm,
/// gửi cảnh cáo hoặc khóa shop (dùng chung IShopService với Người A để có audit log).
/// </summary>
public sealed class ReviewsController(
    AppDbContext db,
    IReviewService reviews,
    IShopService shops,
    INotificationService notifications) : AdminBaseController
{
    private const string TieuDeCanhBao = "Cảnh cáo chất lượng dịch vụ từ Ban Quản Trị";

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ShopCanRaSoatFilterVm filter, CancellationToken cancellationToken)
    {
        var vm = await reviews.LayDanhSachShopCanRaSoatAsync(filter, cancellationToken);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, int page = 1, CancellationToken cancellationToken = default)
    {
        var shop = await LayShopAsync(id, cancellationToken);
        if (shop is null) return NotFound();

        var danhGias = await reviews.LayDanhGiaTheoShopAsync(
            id, page, pageSize: 10, chiLayTatCa: true, cancellationToken);

        // 5 đơn gần nhất để Admin đối chiếu lịch sử giao dịch khi ra quyết định.
        var donGanDay = await db.DonHangs.AsNoTracking()
            .Where(o => o.MaCuaHang == id)
            .OrderByDescending(o => o.NgayTao).ThenByDescending(o => o.MaDonHang)
            .Take(5)
            .Select(o => new DonHangGanDayVm
            {
                MaDonHang    = o.MaDonHang,
                MaDon        = o.MaDon,
                TenNguoiNhan = o.TenNguoiNhan,
                TongTien     = o.TongTien,
                TrangThai    = o.TrangThai,
                NgayTao      = o.NgayTao
            })
            .ToListAsync(cancellationToken);

        return View(new ChiTietShopRaSoatVm
        {
            Shop           = shop,
            DanhGias       = danhGias,
            DonHangsGanDay = donGanDay
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleReviewStatus(
        int maDanhGia, int maCuaHang, string trangThaiMoi, string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.LayNguoiDungId();
        if (adminId is null) return Challenge();

        var trangThaiChuan = (trangThaiMoi ?? string.Empty).Trim().ToUpperInvariant();
        var (success, error) = await reviews.DoiTrangThaiHienThiAsync(
            maDanhGia, trangThaiChuan, adminId.Value, cancellationToken);

        if (success)
        {
            this.ThongBaoThanhCong(trangThaiChuan == "HIDDEN"
                ? "Đã ẩn đánh giá. Điểm trung bình của shop sẽ được tính lại từ các đánh giá VISIBLE còn lại."
                : "Đã hiển thị lại đánh giá. Điểm trung bình của shop sẽ bao gồm đánh giá này.");
        }
        else
        {
            this.ThongBaoLoi(error ?? "Không cập nhật được trạng thái đánh giá.");
        }

        return QuayLai(returnUrl, maCuaHang);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Warn(
        int maCuaHang, string reason, string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (reason.Length == 0)
        {
            this.ThongBaoLoi("Vui lòng nhập nội dung cảnh cáo.");
            return QuayLai(returnUrl, maCuaHang);
        }
        if (reason.Length > 1000)
        {
            this.ThongBaoLoi("Nội dung cảnh cáo tối đa 1 000 ký tự.");
            return QuayLai(returnUrl, maCuaHang);
        }

        var shop = await db.CuaHangs.AsNoTracking()
            .Where(x => x.MaCuaHang == maCuaHang)
            .Select(x => new { x.MaNguoiDung, x.TenShop })
            .FirstOrDefaultAsync(cancellationToken);

        if (shop is null)
        {
            this.ThongBaoLoi("Không tìm thấy cửa hàng.");
            return QuayLai(returnUrl, maCuaHang);
        }

        var (success, error) = await notifications.CreateAsync(
            shop.MaNguoiDung, NotificationTypes.System, TieuDeCanhBao, reason, cancellationToken: cancellationToken);

        if (success)
            this.ThongBaoThanhCong($"Đã gửi cảnh cáo tới chủ shop \"{shop.TenShop}\".");
        else
            this.ThongBaoLoi(error ?? "Không gửi được cảnh cáo.");

        return QuayLai(returnUrl, maCuaHang);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockShop(
        int maCuaHang, string reason, string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.LayNguoiDungId();
        if (adminId is null) return Challenge();

        var (success, error) = await shops.LockShopAsync(
            adminId.Value, maCuaHang, reason, cancellationToken);

        if (success) this.ThongBaoThanhCong("Đã khóa cửa hàng.");
        else this.ThongBaoLoi(error ?? "Không khóa được cửa hàng.");

        return QuayLai(returnUrl, maCuaHang);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockShop(
        int maCuaHang, string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.LayNguoiDungId();
        if (adminId is null) return Challenge();

        var (success, error) = await shops.UnlockShopAsync(adminId.Value, maCuaHang, cancellationToken);

        if (success) this.ThongBaoThanhCong("Đã mở khóa cửa hàng.");
        else this.ThongBaoLoi(error ?? "Không mở khóa được cửa hàng.");

        return QuayLai(returnUrl, maCuaHang);
    }

    private Task<ShopCanRaSoatItemVm?> LayShopAsync(int maCuaHang, CancellationToken cancellationToken) =>
        (from d in db.DiemCuaHangViews.AsNoTracking()
         join ch in db.CuaHangs.AsNoTracking() on d.MaCuaHang equals ch.MaCuaHang
         join nd in db.NguoiDungs.AsNoTracking() on ch.MaNguoiDung equals nd.MaNguoiDung
         join tt in db.TinhThanhs.AsNoTracking() on ch.MaTinhThanh equals (int?)tt.MaTinhThanh into tts
         from tt in tts.DefaultIfEmpty()
         join px in db.PhuongXas.AsNoTracking() on ch.MaPhuongXa equals (int?)px.MaPhuongXa into pxs
         from px in pxs.DefaultIfEmpty()
         where ch.MaCuaHang == maCuaHang
         select new ShopCanRaSoatItemVm
         {
             MaCuaHang       = ch.MaCuaHang,
             TenShop         = ch.TenShop,
             TrangThaiShop   = ch.TrangThai,
             LyDoKhoa        = ch.LyDoKhoa,
             ChuShopId       = nd.MaNguoiDung,
             TenChuShop      = nd.HoTen,
             EmailChuShop    = nd.Email,
             SoDienThoai     = ch.SoDienThoaiLienHe,
             KhuVuc          = px != null && tt != null
                 ? px.TenPhuongXa + ", " + tt.TenTinhThanh
                 : (ch.KhuVucHienThi ?? "Chưa cập nhật"),
             DiemTrungBinh   = d.DiemTrungBinh,
             SoDanhGia       = d.SoDanhGia,
             SoDonThanhCong  = d.SoDonThanhCong,
             SoBaoCao        = db.BaoCaos.Count(b =>
                 (b.LoaiDoiTuong == "SHOP" && b.MaDoiTuong == ch.MaCuaHang) ||
                 (b.LoaiDoiTuong == "PRODUCT" &&
                  db.SanPhams.Any(s => s.MaSanPham == b.MaDoiTuong && s.MaCuaHang == ch.MaCuaHang))),
             LaShopCanRaSoat = d.DiemTrungBinh < 3.0m && d.SoDanhGia >= 5
         }).FirstOrDefaultAsync(cancellationToken);

    /// <summary>Chỉ chấp nhận URL nội bộ để tránh open redirect; mặc định quay về trang chi tiết.</summary>
    private IActionResult QuayLai(string? returnUrl, int maCuaHang) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Details), new { id = maCuaHang });
}
