using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Services.Disputes;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public class DisputesController : AdminBaseController
{
    private readonly IDisputeService _disputeService;

    public DisputesController(IDisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    /// <summary>Danh sách tranh chấp và khiếu nại trong toàn sàn.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? trangThai = null, int page = 1)
    {
        var vm = await _disputeService.LayDanhSachTranhChapAdminAsync(trangThai, page);
        return View(vm);
    }

    /// <summary>Chi tiết một tranh chấp.</summary>
    [HttpGet]
    public async Task<IActionResult> ChiTiet(int id)
    {
        var vm = await _disputeService.LayChiTietTranhChapAsync(id);
        if (vm == null)
        {
            this.ThongBaoLoi("Không tìm thấy tranh chấp.");
            return RedirectToAction(nameof(Index));
        }
        return View(vm);
    }

    /// <summary>Admin đưa ra phán quyết giải quyết tranh chấp.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PhanQuyet(int maTranhChap, string phanQuyet, string? ghiChu = null)
    {
        var adminId = User.LayNguoiDungId()!.Value;
        var (thanhCong, thongBao) = await _disputeService.XuLyTranhChapAdminAsync(maTranhChap, adminId, phanQuyet, ghiChu);

        if (thanhCong)
        {
            this.ThongBaoThanhCong(thongBao ?? "Đã xử lý tranh chấp thành công.");
        }
        else
        {
            this.ThongBaoLoi(thongBao ?? "Không thể xử lý tranh chấp.");
        }

        return RedirectToAction(nameof(Index));
    }
}
