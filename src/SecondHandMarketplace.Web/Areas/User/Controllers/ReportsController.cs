using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Models.ViewModels.Reports;
using SecondHandMarketplace.Web.Services.Reports;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

public class ReportsController : UserBaseController
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>Lịch sử các báo cáo vi phạm người dùng đã gửi.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.LayNguoiDungId()!.Value;
        var items = await _reportService.LayLichSuBaoCaoCuaToiAsync(userId);
        return View(items);
    }

    /// <summary>Hiển thị trang tạo báo cáo vi phạm.</summary>
    [HttpGet]
    public IActionResult Tao(string? loai, int id, string? ten = null, string? returnUrl = null)
    {
        if (id <= 0)
        {
            return RedirectToAction(nameof(Index));
        }

        ViewBag.ReturnUrl = (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) ? returnUrl : null;
        var vm = new TaoBaoCaoViewModel
        {
            LoaiDoiTuong = string.IsNullOrWhiteSpace(loai) ? "PRODUCT" : loai.ToUpper(),
            MaDoiTuong = id,
            TenDoiTuong = ten
        };
        return View(vm);
    }

    /// <summary>Xử lý gửi báo cáo vi phạm.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tao(TaoBaoCaoViewModel vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) ? returnUrl : null;
            return View(vm);
        }

        var userId = User.LayNguoiDungId()!.Value;
        var (thanhCong, thongBao) = await _reportService.TaoBaoCaoAsync(userId, vm);

        if (thanhCong)
        {
            this.ThongBaoThanhCong(thongBao ?? "Gửi báo cáo thành công.");
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        this.ThongBaoLoi(thongBao ?? "Không thể gửi báo cáo.");
        ViewBag.ReturnUrl = (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) ? returnUrl : null;
        return View(vm);
    }
}
