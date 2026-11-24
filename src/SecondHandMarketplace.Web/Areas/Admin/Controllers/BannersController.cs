using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.AuditLogs;
using SecondHandMarketplace.Web.Services.Banners;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

/// <summary>B5 – Admin quản lý banner trang chủ.</summary>
public sealed class BannersController(
    IBannerService banners,
    IAuditLogService auditLog,
    IWebHostEnvironment env) : AdminBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(new BannerManagementVm { Banners = await banners.LayTatCaAsync(cancellationToken) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int? id, string tieuDe, string? moTa, string? duongDanAnh, string? lienKet,
        int thuTu = 1, bool dangHienThi = true, DateTime? batDauTu = null, DateTime? ketThucLuc = null,
        IFormFile? anhTaiLen = null, CancellationToken cancellationToken = default)
    {
        // Chọn file thì ảnh upload thắng đường dẫn nhập tay; Magic Bytes kiểm tra trước khi ghi đĩa.
        string? duongDanVuaTao = null;
        if (anhTaiLen is { Length: > 0 })
        {
            var (hopLe, loi, extension) = await UploadAnhAnToan.KiemTraAnhHopLeAsync(anhTaiLen, cancellationToken: cancellationToken);
            if (!hopLe)
            {
                TempData["Loi"] = loi ?? "Tệp ảnh không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var webRoot = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var (duongDan, fullPath) = await UploadAnhAnToan.LuuAnhAsync(anhTaiLen, webRoot, "banners", extension!, cancellationToken);
            duongDanAnh = duongDan;
            duongDanVuaTao = fullPath;
        }

        var result = await banners.SaveAsync(id, tieuDe, moTa, duongDanAnh ?? string.Empty, lienKet, thuTu, dangHienThi,
            SangUtc(batDauTu), SangUtc(ketThucLuc), cancellationToken);
        if (!result.Success)
        {
            UploadAnhAnToan.XoaAnh(duongDanVuaTao);
            TempData["Loi"] = result.Error;
        }
        else
        {
            TempData["ThanhCong"] = "Đã lưu banner.";
            await LogAsync("SAVE_BANNER", result.Id);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await banners.DeleteAsync(id, cancellationToken);
        if (!result.Success) TempData["Loi"] = result.Error;
        else
        {
            TempData["ThanhCong"] = result.Error;
            await LogAsync("DELETE_BANNER", id);
        }
        return RedirectToAction(nameof(Index));
    }

    // Input datetime-local là giờ Việt Nam; DB lưu UTC (nhất quán với phần còn lại).
    private static DateTime? SangUtc(DateTime? gioVn) => gioVn is null ? null : HienThi.TuGioVnSangUtc(gioVn.Value);

    private Task LogAsync(string action, int id) =>
        auditLog.LogAsync(User.LayNguoiDungId(), action, "Banner", id);
}

public sealed class BannerManagementVm
{
    public IReadOnlyList<Banner> Banners { get; set; } = [];
}
