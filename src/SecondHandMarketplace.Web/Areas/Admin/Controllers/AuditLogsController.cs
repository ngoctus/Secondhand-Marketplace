using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.ViewModels.Admin;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public sealed class AuditLogsController(AppDbContext db) : AdminBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(string? userId, string? action, DateTime? from, DateTime? to, int page = 1, CancellationToken cancellationToken = default)
    {
        const int pageSize = 25;
        page = Math.Max(1, page);
        var query = from log in db.NhatKyHeThongs.AsNoTracking()
                    join user in db.NguoiDungs.AsNoTracking() on log.MaNguoiDung equals (int?)user.MaNguoiDung into users
                    from user in users.DefaultIfEmpty()
                    select new { log, UserName = user == null ? null : user.HoTen };
        if (int.TryParse(userId, out var id)) query = query.Where(x => x.log.MaNguoiDung == id);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.log.HanhDong.Contains(action.Trim()));
        if (from.HasValue) { var start = HienThi.TuGioVnSangUtc(from.Value.Date); query = query.Where(x => x.log.ThoiGian >= start); }
        if (to.HasValue) { var end = HienThi.TuGioVnSangUtc(to.Value.Date.AddDays(1)); query = query.Where(x => x.log.ThoiGian < end); }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.log.ThoiGian).ThenByDescending(x => x.log.MaNhatKyHeThong)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AuditLogRowVm { Id = x.log.MaNhatKyHeThong, UserId = x.log.MaNguoiDung,
                UserName = x.UserName, Action = x.log.HanhDong, ObjectType = x.log.LoaiDoiTuong,
                ObjectId = x.log.DoiTuongId, Result = x.log.KetQua, Reason = x.log.LyDo,
                Detail = x.log.ChiTiet, Time = x.log.ThoiGian }).ToListAsync(cancellationToken);
        return View(new AuditLogsVm { Items = items, UserId = userId, Action = action, From = from, To = to,
            Page = page, TotalItems = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }
}
