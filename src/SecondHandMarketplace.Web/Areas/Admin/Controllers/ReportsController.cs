using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Services.Reports;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public class ReportsController : AdminBaseController
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>Danh sách báo cáo vi phạm trong hệ thống.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? trangThai = null, string? loaiDoiTuong = null, int page = 1)
    {
        var vm = await _reportService.LayDanhSachBaoCaoAdminAsync(trangThai, loaiDoiTuong, page);
        return View(vm);
    }

    /// <summary>Admin xử lý báo cáo: DISMISS (bỏ qua) hoặc RESOLVE (xử lý đối tượng vi phạm).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XuLy(int maBaoCao, string hanhDong, string? ghiChu = null)
    {
        var adminId = User.LayNguoiDungId()!.Value;
        var (thanhCong, thongBao) = await _reportService.XuLyBaoCaoAdminAsync(maBaoCao, adminId, hanhDong, ghiChu);

        if (thanhCong)
        {
            this.ThongBaoThanhCong(thongBao ?? "Xử lý báo cáo thành công.");
        }
        else
        {
            this.ThongBaoLoi(thongBao ?? "Không thể xử lý báo cáo.");
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Danh sách báo cáo kỹ thuật (lỗi web/hệ thống) do người dùng gửi.</summary>
    [HttpGet]
    public async Task<IActionResult> Technical(string? trangThai = null, int page = 1)
    {
        var vm = await _reportService.LayDanhSachBaoCaoAdminAsync(trangThai, "TECHNICAL", page);
        return View(vm);
    }

    /// <summary>Admin xử lý báo cáo kỹ thuật và gửi thông báo riêng cho người dùng đã báo cáo.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PhanHoiKyThuat(int maBaoCao, string hanhDong, string? tieuDeThongBao, string? noiDungThongBao, string? trangThaiFilter = null, int page = 1)
    {
        var adminId = User.LayNguoiDungId()!.Value;
        var (thanhCong, thongBao) = await _reportService.PhanHoiBaoCaoKyThuatAdminAsync(maBaoCao, adminId, hanhDong, tieuDeThongBao, noiDungThongBao);

        if (thanhCong)
        {
            this.ThongBaoThanhCong(thongBao ?? "Đã phản hồi báo cáo kỹ thuật và gửi thông báo riêng cho người dùng.");
        }
        else
        {
            this.ThongBaoLoi(thongBao ?? "Không thể phản hồi báo cáo kỹ thuật.");
        }

        return RedirectToAction(nameof(Technical), new { trangThai = trangThaiFilter, page });
    }
}
