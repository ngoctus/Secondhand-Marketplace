using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Models.ViewModels.Disputes;
using SecondHandMarketplace.Web.Services.Disputes;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

public class DisputesController : UserBaseController
{
    private readonly IDisputeService _disputeService;
    private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

    public DisputesController(IDisputeService disputeService, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
    {
        _disputeService = disputeService;
        _env = env;
    }

    /// <summary>Danh sách các khiếu nại / tranh chấp liên quan đến người dùng (cả tư cách người mua lẫn chủ shop bán hàng).</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.LayNguoiDungId();
        if (userId == null) return Challenge();

        var list = await _disputeService.LayDanhSachTranhChapNguoiDungAsync(userId.Value);
        return View(list);
    }

    /// <summary>Xem chi tiết một khiếu nại tranh chấp.</summary>
    [HttpGet]
    public async Task<IActionResult> ChiTiet(int id)
    {
        var userId = User.LayNguoiDungId();
        if (userId == null) return Challenge();

        var vm = await _disputeService.LayChiTietTranhChapAsync(id);
        if (vm == null)
        {
            this.ThongBaoLoi("Không tìm thấy khiếu nại tranh chấp.");
            return RedirectToAction(nameof(Index));
        }

        // Kiểm tra bảo mật: chỉ người mua hoặc chủ shop đơn hàng mới được xem
        if (vm.MaNguoiTao != userId.Value && vm.MaChuShop != userId.Value)
        {
            this.ThongBaoLoi("Bạn không có quyền truy cập khiếu nại này.");
            return RedirectToAction(nameof(Index));
        }

        return View(vm);
    }

    /// <summary>Chủ shop gửi phản hồi đối với khiếu nại của người mua kèm ảnh chứng minh nếu có.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PhanHoi(int id, string phanHoi, IFormFile? fileDinhKem)
    {
        var userId = User.LayNguoiDungId();
        if (userId == null) return Challenge();

        string? createdFilePath = null;
        string? anhPhanHoi = null;

        if (fileDinhKem != null && fileDinhKem.Length > 0)
        {
            var (hopLe, loi, ext) = await UploadAnhAnToan.KiemTraAnhHopLeAsync(fileDinhKem);
            if (!hopLe)
            {
                this.ThongBaoLoi(loi ?? "Tệp ảnh không hợp lệ.");
                return RedirectToAction(nameof(ChiTiet), new { id });
            }

            var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var (relPath, fullPath) = await UploadAnhAnToan.LuuAnhAsync(fileDinhKem, webRoot, "disputes", ext!);
            anhPhanHoi = relPath;
            createdFilePath = fullPath;
        }

        var (thanhCong, thongBao) = await _disputeService.NguoiBanPhanHoiAsync(userId.Value, id, phanHoi, anhPhanHoi);
        if (thanhCong)
        {
            this.ThongBaoThanhCong(thongBao ?? "Đã gửi phản hồi khiếu nại thành công.");
        }
        else
        {
            UploadAnhAnToan.XoaAnh(createdFilePath);
            this.ThongBaoLoi(thongBao ?? "Không thể gửi phản hồi.");
        }

        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    /// <summary>Trang mở khiếu nại tranh chấp từ đơn hàng.</summary>
    [HttpGet]
    public async Task<IActionResult> Tao(int maDonHang)
    {
        var userId = User.LayNguoiDungId();
        if (userId == null) return Challenge();

        var (hopLe, thongBao, vm, maTranhChapHienCo) = await _disputeService.ChuanBiMoTranhChapAsync(userId.Value, maDonHang);
        if (!hopLe)
        {
            this.ThongBaoLoi(thongBao ?? "Không thể mở khiếu nại.");
            if (maTranhChapHienCo.HasValue)
            {
                return RedirectToAction(nameof(ChiTiet), new { id = maTranhChapHienCo.Value });
            }
            return RedirectToAction("Index", "Orders");
        }

        return View(vm);
    }

    /// <summary>Xử lý submit mở khiếu nại tranh chấp.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tao(MoTranhChapViewModel vm)
    {
        var userId = User.LayNguoiDungId();
        if (userId == null) return Challenge();

        string? createdFilePath = null;

        // Xử lý upload tệp ảnh bằng chứng nếu có
        if (vm.FileDinhKem != null && vm.FileDinhKem.Length > 0)
        {
            var (hopLe, loi, ext) = await UploadAnhAnToan.KiemTraAnhHopLeAsync(vm.FileDinhKem);
            if (!hopLe)
            {
                ModelState.AddModelError(nameof(vm.FileDinhKem), loi ?? "Tệp ảnh không hợp lệ.");
            }
            else
            {
                var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var (relPath, fullPath) = await UploadAnhAnToan.LuuAnhAsync(vm.FileDinhKem, webRoot, "disputes", ext!);
                vm.BangChungAnh = relPath;
                createdFilePath = fullPath;
            }
        }

        if (!ModelState.IsValid)
        {
            UploadAnhAnToan.XoaAnh(createdFilePath);
            return View(vm);
        }

        var (thanhCong, thongBao) = await _disputeService.MoTranhChapAsync(userId.Value, vm);
        if (thanhCong)
        {
            this.ThongBaoThanhCong(thongBao ?? "Đã gửi khiếu nại thành công.");
            return RedirectToAction(nameof(Index));
        }

        UploadAnhAnToan.XoaAnh(createdFilePath);

        this.ThongBaoLoi(thongBao ?? "Không thể mở khiếu nại.");
        return View(vm);
    }
}
