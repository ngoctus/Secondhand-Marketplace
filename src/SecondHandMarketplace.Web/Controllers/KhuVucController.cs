using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Services.Areas;

namespace SecondHandMarketplace.Web.Controllers;

/// <summary>
/// Khu vực dùng chung: không cần đăng nhập, chỉ trả/ghi tên địa danh (không GPS, không dữ liệu cá nhân).
/// </summary>
[AllowAnonymous]
public sealed class KhuVucController(IKhuVucService khuVuc) : Controller
{
    /// <summary>GET /KhuVuc/PhuongXa?maTinh=79 → [{ "ma": 26734, "ten": "Phường Sài Gòn" }, ...]</summary>
    [HttpGet]
    public async Task<IActionResult> PhuongXa(int maTinh, CancellationToken cancellationToken)
    {
        if (maTinh <= 0) return Json(Array.Empty<object>());
        var items = await khuVuc.GetPhuongXaTheoTinhAsync(maTinh, cancellationToken);
        return Json(items.Select(x => new { ma = x.Ma, ten = x.Ten }));
    }

    /// <summary>Lưu khu vực đã chọn vào cookie rồi quay lại trang trước (returnUrl chỉ nhận đường dẫn nội bộ).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(int? maTinh, int? maXa, string? returnUrl, CancellationToken cancellationToken)
    {
        var ok = await khuVuc.LuuKhuVucDaChonAsync(HttpContext, maTinh, maXa, cancellationToken);
        if (!ok) TempData["Loi"] = "Khu vực đã chọn không hợp lệ.";

        if (UrlAnToan.HopLe(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl!);
        return RedirectToAction("Index", "Home");
    }
}
