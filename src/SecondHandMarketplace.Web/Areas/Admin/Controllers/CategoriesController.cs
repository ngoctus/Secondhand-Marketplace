using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Services.AuditLogs;
using SecondHandMarketplace.Web.Services.Categories;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public sealed class CategoriesController(ICategoryService categories, IAuditLogService auditLog) : AdminBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(new CategoryManagementVm { Categories = await categories.GetCategoriesAsync(cancellationToken), Conditions = await categories.GetConditionsAsync(cancellationToken) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(int? id, string name, int order = 0, bool visible = true, CancellationToken cancellationToken = default)
    {
        var result = await categories.SaveCategoryAsync(id, name, order, visible, cancellationToken);
        if (!result.Success) TempData["Loi"] = result.Error;
        else { TempData["ThanhCong"] = "Đã lưu danh mục."; await LogAsync("SAVE_CATEGORY", "DanhMuc", result.Id); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
    {
        var result = await categories.DeleteCategoryAsync(id, cancellationToken);
        if (!result.Success) TempData["Loi"] = result.Error;
        else { TempData["ThanhCong"] = result.Error; await LogAsync("DELETE_CATEGORY", "DanhMuc", id); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCondition(int? id, string code, string name, string? description, int order = 0, bool active = true, CancellationToken cancellationToken = default)
    {
        var result = await categories.SaveConditionAsync(id, code, name, description, order, active, cancellationToken);
        if (!result.Success) TempData["Loi"] = result.Error;
        else { TempData["ThanhCong"] = "Đã lưu tình trạng sản phẩm."; await LogAsync("SAVE_PRODUCT_CONDITION", "TinhTrangSanPham", result.Id); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCondition(int id, CancellationToken cancellationToken)
    {
        var result = await categories.DeleteConditionAsync(id, cancellationToken);
        if (!result.Success) TempData["Loi"] = result.Error;
        else { TempData["ThanhCong"] = result.Error; await LogAsync("DELETE_PRODUCT_CONDITION", "TinhTrangSanPham", id); }
        return RedirectToAction(nameof(Index));
    }

    private Task LogAsync(string action, string objectType, int id) =>
        auditLog.LogAsync(int.TryParse(User.GetUserId(), out var userId) ? userId : null, action, objectType, id);
}

public sealed class CategoryManagementVm
{
    public IReadOnlyList<SecondHandMarketplace.Web.Models.Entities.DanhMuc> Categories { get; set; } = [];
    public IReadOnlyList<SecondHandMarketplace.Web.Models.Entities.TinhTrangSanPham> Conditions { get; set; } = [];
}