using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Models.ViewModels.Disputes;
using SecondHandMarketplace.Web.Services.Disputes;
using SecondHandMarketplace.Web.Services.Storage;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

public class DisputesController : UserBaseController
{
    private readonly IDisputeService _disputeService;
    private readonly IUploadStorage _uploadStorage;

    public DisputesController(IDisputeService disputeService, IUploadStorage uploadStorage)
    {
        _disputeService = disputeService;
        _uploadStorage = uploadStorage;
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
    public async Task<IActionResult> PhanHoi(int id, string phanHoi, IFormFile? fileDinhKem, CancellationToken cancellationToken = default)
    {
        var userId = User.LayNguoiDungId();
        if (userId == null) return Challenge();

        string? createdFilePath = null;
        string? anhPhanHoi = null;

        if (fileDinhKem != null && fileDinhKem.Length > 0)
        {
            var (hopLe, loi, ext) = await UploadAnhAnToan.KiemTraAnhHopLeAsync(fileDinhKem, cancellationToken: cancellationToken);
            if (!hopLe)
            {
                this.ThongBaoLoi(loi ?? "Tệp ảnh không hợp lệ.");
                return RedirectToAction(nameof(ChiTiet), new { id });
            }

            anhPhanHoi = $"/uploads/disputes/{Guid.NewGuid():N}{ext}";
            await using var stream = fileDinhKem.OpenReadStream();
            await _uploadStorage.SaveAsync(stream, anhPhanHoi, isPrivate: true, UploadContentType(ext!), cancellationToken);
            createdFilePath = anhPhanHoi;
        }

        var (thanhCong, thongBao) = await _disputeService.NguoiBanPhanHoiAsync(userId.Value, id, phanHoi, anhPhanHoi);
        if (thanhCong)
        {
            this.ThongBaoThanhCong(thongBao ?? "Đã gửi phản hồi khiếu nại thành công.");
        }
        else
        {
            if (createdFilePath is not null)
                await _uploadStorage.DeleteAsync(createdFilePath, isPrivate: true, cancellationToken);
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
    public async Task<IActionResult> Tao(MoTranhChapViewModel vm, CancellationToken cancellationToken = default)
    {
        var userId = User.LayNguoiDungId();
        if (userId == null) return Challenge();

        string? createdFilePath = null;

        // Xử lý upload tệp ảnh bằng chứng nếu có
        if (vm.FileDinhKem != null && vm.FileDinhKem.Length > 0)
        {
            var (hopLe, loi, ext) = await UploadAnhAnToan.KiemTraAnhHopLeAsync(vm.FileDinhKem, cancellationToken: cancellationToken);
            if (!hopLe)
            {
                ModelState.AddModelError(nameof(vm.FileDinhKem), loi ?? "Tệp ảnh không hợp lệ.");
            }
            else
            {
                vm.BangChungAnh = $"/uploads/disputes/{Guid.NewGuid():N}{ext}";
                await using var stream = vm.FileDinhKem.OpenReadStream();
                await _uploadStorage.SaveAsync(stream, vm.BangChungAnh, isPrivate: true, UploadContentType(ext!), cancellationToken);
                createdFilePath = vm.BangChungAnh;
            }
        }

        if (!ModelState.IsValid)
        {
            if (createdFilePath is not null)
                await _uploadStorage.DeleteAsync(createdFilePath, isPrivate: true, cancellationToken);
            return View(vm);
        }

        var (thanhCong, thongBao) = await _disputeService.MoTranhChapAsync(userId.Value, vm);
        if (thanhCong)
        {
            this.ThongBaoThanhCong(thongBao ?? "Đã gửi khiếu nại thành công.");
            return RedirectToAction(nameof(Index));
        }

        if (createdFilePath is not null)
            await _uploadStorage.DeleteAsync(createdFilePath, isPrivate: true, cancellationToken);

        this.ThongBaoLoi(thongBao ?? "Không thể mở khiếu nại.");
        return View(vm);
    }

    private static string UploadContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };
}
