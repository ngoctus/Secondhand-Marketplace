using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.ViewModels.Admin;
using SecondHandMarketplace.Web.Services.Accounts;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public sealed class UsersController(AppDbContext db, IUserAdminService userAdmin) : AdminBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, int page = 1, CancellationToken cancellationToken = default)
    {
        const int pageSize = 15;
        page = Math.Max(1, page);
        var query = db.NguoiDungs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => x.HoTen.Contains(keyword) || x.Email.Contains(keyword));
        }
        if (status is "ACTIVE" or "LOCKED") query = query.Where(x => x.TrangThai == status);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.NgayTao).ThenByDescending(x => x.MaNguoiDung)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AdminUserRowVm
            {
                Id = x.MaNguoiDung,
                HoTen = x.HoTen,
                Email = x.Email,
                SoDienThoai = x.SoDienThoai,
                VaiTro = x.VaiTro,
                TrangThai = x.TrangThai,
                LyDoKhoa = x.LyDoKhoa,
                NgayTao = x.NgayTao
            }).ToListAsync(cancellationToken);
        return View(new AdminUsersVm
        {
            Items = rows,
            Search = search,
            Status = status,
            Page = page,
            TotalItems = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(int id, string reason, CancellationToken cancellationToken)
    {
        var result = await userAdmin.LockUserAsync(ParseCurrentUserId(), id, reason, cancellationToken);
        if (result.Success) TempData["ThanhCong"] = "Đã khóa tài khoản.";
        else TempData["Loi"] = result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(int id, CancellationToken cancellationToken)
    {
        var result = await userAdmin.UnlockUserAsync(ParseCurrentUserId(), id, cancellationToken);
        if (result.Success) TempData["ThanhCong"] = "Đã mở khóa tài khoản.";
        else TempData["Loi"] = result.Error;
        return RedirectToAction(nameof(Index));
    }

    private int? ParseCurrentUserId() => int.TryParse(User.GetUserId(), out var id) ? id : null;
}