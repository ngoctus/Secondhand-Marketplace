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

    /// <summary>Chủ shop gửi phản hồi đối với khiếu nại của người mua.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PhanHoi(int id, string phanHoi)
    {
        var userId = User.LayNguoiDungId();
        if (userId == null) return Challenge();

        var (thanhCong, thongBao) = await _disputeService.NguoiBanPhanHoiAsync(userId.Value, id, phanHoi);
        if (thanhCong)
        {
            this.ThongBaoThanhCong(thongBao ?? "Đã gửi phản hồi khiếu nại thành công.");
        }
        else
        {
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
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(vm.FileDinhKem.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                ModelState.AddModelError(nameof(vm.FileDinhKem), "Chỉ chấp nhận tệp ảnh JPG, JPEG, PNG hoặc WEBP.");
            }
            else if (vm.FileDinhKem.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(vm.FileDinhKem), "Kích thước ảnh không được vượt quá 5MB.");
            }
            else
            {
                // Kiểm tra Magic Bytes chống file giả mạo đuôi (đọc 12 bytes)
                using var readStream = vm.FileDinhKem.OpenReadStream();
                var header = new byte[12];
                var readBytes = await readStream.ReadAsync(header, 0, 12);
                bool isImage = false;
                if (readBytes >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) 
                    isImage = true; // JPEG/JPG
                else if (readBytes >= 4 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) 
                    isImage = true; // PNG
                else if (readBytes >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                                        && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50) 
                    isImage = true; // WEBP (RIFF....WEBP)

                if (!isImage)
                {
                    ModelState.AddModelError(nameof(vm.FileDinhKem), "Nội dung tệp không phải là định dạng hình ảnh hợp lệ.");
                }
                else
                {
                    var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var folder = Path.Combine(webRoot, "uploads", "disputes");
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    createdFilePath = Path.Combine(folder, fileName);
                    readStream.Position = 0;
                    using (var writeStream = new FileStream(createdFilePath, FileMode.Create))
                    {
                        await readStream.CopyToAsync(writeStream);
                    }
                    vm.BangChungAnh = $"/uploads/disputes/{fileName}";
                }
            }
        }

        if (!ModelState.IsValid)
        {
            if (createdFilePath != null && System.IO.File.Exists(createdFilePath))
            {
                try { System.IO.File.Delete(createdFilePath); } catch { /* Best effort */ }
            }
            return View(vm);
        }

        var (thanhCong, thongBao) = await _disputeService.MoTranhChapAsync(userId.Value, vm);
        if (thanhCong)
        {
            this.ThongBaoThanhCong(thongBao ?? "Đã gửi khiếu nại thành công.");
            return RedirectToAction(nameof(Index));
        }

        if (createdFilePath != null && System.IO.File.Exists(createdFilePath))
        {
            try { System.IO.File.Delete(createdFilePath); } catch { /* Best effort */ }
        }

        this.ThongBaoLoi(thongBao ?? "Không thể mở khiếu nại.");
        return View(vm);
    }
}
