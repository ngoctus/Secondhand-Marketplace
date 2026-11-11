using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Models.ViewModels.Disputes;
using SecondHandMarketplace.Web.Models.ViewModels.Orders;
using SecondHandMarketplace.Web.Services.Disputes;
using SecondHandMarketplace.Web.Services.Orders;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

public class OrdersController : UserBaseController
{
    private readonly IOrderService _orderService;
    private readonly IDisputeService _disputeService;

    public OrdersController(IOrderService orderService, IDisputeService disputeService)
    {
        _orderService = orderService;
        _disputeService = disputeService;
    }

    /// <summary>Danh sách đơn mua của người dùng.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? trangThai = null)
    {
        var userId = User.LayNguoiDungId()!.Value;
        ViewBag.TrangThaiHienTai = trangThai;
        var list = await _orderService.LayDanhSachDonMuaAsync(userId, trangThai);
        return View(list);
    }

    /// <summary>Xem chi tiết một đơn mua.</summary>
    [HttpGet]
    public async Task<IActionResult> ChiTiet(int id)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var vm = await _orderService.LayChiTietDonMuaAsync(userId, id);
        if (vm == null)
        {
            this.ThongBaoLoi("Không tìm thấy đơn hàng.");
            return RedirectToAction(nameof(Index));
        }
        return View(vm);
    }

    /// <summary>Màn hình thanh toán (Checkout từ giỏ hoặc Mua ngay).</summary>
    [HttpGet]
    public async Task<IActionResult> Checkout(int? maSanPham = null)
    {
        var userId = User.LayNguoiDungId()!.Value;
        CheckoutViewModel vm;

        if (maSanPham.HasValue && maSanPham.Value > 0)
        {
            var muaNgayVm = await _orderService.ChuanBiCheckoutMuaNgayAsync(userId, maSanPham.Value);
            if (muaNgayVm == null)
            {
                this.ThongBaoLoi("Sản phẩm không khả dụng hoặc bạn không thể mua sản phẩm của chính mình.");
                return RedirectToAction("Index", "Cart");
            }
            vm = muaNgayVm;
        }
        else
        {
            vm = await _orderService.ChuanBiCheckoutTuGioAsync(userId);
            if (vm.GioHang == null || !vm.GioHang.DanhSachNhomShop.Any())
            {
                this.ThongBaoLoi("Bạn chưa chọn món đồ nào trong giỏ hàng để thanh toán.");
                return RedirectToAction("Index", "Cart");
            }
        }

        return View(vm);
    }

    /// <summary>Xử lý đặt hàng.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DatHang(CheckoutViewModel model)
    {
        var userId = User.LayNguoiDungId()!.Value;

        if (model.HinhThucNhan == "GAP_TRUC_TIEP" || model.PhiVanChuyen < 0)
        {
            model.PhiVanChuyen = 0;
        }

        if (!ModelState.IsValid)
        {
            if (model.MaSanPhamMuaNgay.HasValue)
            {
                var prep = await _orderService.ChuanBiCheckoutMuaNgayAsync(userId, model.MaSanPhamMuaNgay.Value);
                model.SanPhamMuaNgay = prep?.SanPhamMuaNgay;
            }
            else
            {
                var prep = await _orderService.ChuanBiCheckoutTuGioAsync(userId);
                model.GioHang = prep.GioHang;
            }
            return View("Checkout", model);
        }

        DatHangResultViewModel result;
        if (model.MaSanPhamMuaNgay.HasValue && model.MaSanPhamMuaNgay.Value > 0)
        {
            result = await _orderService.MuaNgayAsync(userId, model.MaSanPhamMuaNgay.Value, model);
        }
        else
        {
            result = await _orderService.DatHangTuGioAsync(userId, model);
        }

        if (result.ThanhCong)
        {
            this.ThongBaoThanhCong(result.ThongBao ?? "Đặt hàng thành công!");
            return RedirectToAction(nameof(XacNhan), new { maDon = string.Join(",", result.DanhSachMaDon) });
        }

        this.ThongBaoLoi(result.ThongBao ?? "Không thể đặt hàng. Sản phẩm có thể đã được người khác mua trước.");
        return RedirectToAction(nameof(Checkout), new { maSanPham = model.MaSanPhamMuaNgay });
    }

    /// <summary>Màn hình xác nhận đặt hàng thành công.</summary>
    [HttpGet]
    public async Task<IActionResult> XacNhan(string? maDon)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var listMa = (maDon ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        ViewBag.DanhSachMaDon = listMa.ToArray();

        var donHangs = await _orderService.LayDanhSachDonTheoMaAsync(userId, listMa);
        return View(donHangs);
    }

    /// <summary>Xác nhận thanh toán chuyển khoản giả lập.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhanThanhToan(int? maDonHang, string? dsMaDonHang, string? returnUrl = null)
    {
        var userId = User.LayNguoiDungId()!.Value;
        if (!string.IsNullOrEmpty(dsMaDonHang))
        {
            var ids = dsMaDonHang.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                                 .Where(id => id > 0)
                                 .ToList();
            if (ids.Count > 0)
            {
                var (okAll, msgAll) = await _orderService.XacNhanThanhToanTatCaChuyenKhoanAsync(userId, ids);
                if (okAll) this.ThongBaoThanhCong(msgAll ?? "Đã xác nhận thanh toán chuyển khoản.");
                else this.ThongBaoLoi(msgAll ?? "Không thể xác nhận thanh toán.");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction(nameof(Index));
            }
        }

        if (maDonHang.HasValue)
        {
            var (ok, msg) = await _orderService.XacNhanThanhToanChuyenKhoanAsync(userId, maDonHang.Value);
            if (ok) this.ThongBaoThanhCong(msg ?? "Đã xác nhận thanh toán chuyển khoản.");
            else this.ThongBaoLoi(msg ?? "Không thể xác nhận thanh toán.");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(ChiTiet), new { id = maDonHang.Value });
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Người mua yêu cầu hủy đơn.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HuyDon(int maDonHang, string lyDo)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var (thanhCong, thongBao) = await _orderService.HuyDonMuaAsync(userId, maDonHang, lyDo);

        if (thanhCong)
        {
            this.ThongBaoThanhCong(thongBao ?? "Đã hủy đơn hàng thành công.");
        }
        else
        {
            this.ThongBaoLoi(thongBao ?? "Không thể hủy đơn hàng.");
        }

        return RedirectToAction(nameof(ChiTiet), new { id = maDonHang });
    }

    /// <summary>Người mua xác nhận đã nhận được hàng.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhanDaNhanHang(int maDonHang)
    {
        var userId = User.LayNguoiDungId()!.Value;
        var (thanhCong, thongBao) = await _orderService.XacNhanDaNhanHangAsync(userId, maDonHang);

        if (thanhCong)
        {
            this.ThongBaoThanhCong(thongBao ?? "Xác nhận nhận hàng thành công. Cảm ơn bạn!");
        }
        else
        {
            this.ThongBaoLoi(thongBao ?? "Không thể xác nhận nhận hàng.");
        }

        return RedirectToAction(nameof(ChiTiet), new { id = maDonHang });
    }

}
