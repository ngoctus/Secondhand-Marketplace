using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.ViewModels.Admin;
using SecondHandMarketplace.Web.Services.Moderation;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

/// <summary>C2: quản lý từ khóa cấm (chỉ ADMIN nhờ AdminBaseController). Không có thao tác xóa.</summary>
public sealed class ForbiddenKeywordsController(IForbiddenKeywordService keywords) : AdminBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? level, bool? active, CancellationToken cancellationToken) =>
        View(new ForbiddenKeywordsVm
        {
            Items = await keywords.ListAsync(search, level, active, cancellationToken),
            Search = search,
            Level = level,
            Active = active
        });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string phrase, string? group, string level, string? note, CancellationToken cancellationToken)
    {
        var result = await keywords.AddAsync(phrase, group, level, note, ActorId(), cancellationToken);
        if (result.Success) TempData["ThanhCong"] = "Đã thêm từ khóa cấm.";
        else TempData["Loi"] = result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string? group, string level, string? note, CancellationToken cancellationToken)
    {
        var result = await keywords.UpdateAsync(id, group, level, note, ActorId(), cancellationToken);
        if (result.Success) TempData["ThanhCong"] = "Đã cập nhật từ khóa.";
        else TempData["Loi"] = result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, bool active, CancellationToken cancellationToken)
    {
        var result = await keywords.SetActiveAsync(id, active, ActorId(), cancellationToken);
        if (result.Success) TempData["ThanhCong"] = active ? "Đã bật từ khóa." : "Đã tắt từ khóa.";
        else TempData["Loi"] = result.Error;
        return RedirectToAction(nameof(Index));
    }

    private int? ActorId() => int.TryParse(User.GetUserId(), out var id) ? id : null;
}
