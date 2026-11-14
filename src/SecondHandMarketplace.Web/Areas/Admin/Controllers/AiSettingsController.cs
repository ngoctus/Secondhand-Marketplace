using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.ViewModels.Ai;
using SecondHandMarketplace.Web.Services.Ai;
using SecondHandMarketplace.Web.Services.AuditLogs;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

/// <summary>
/// Admin cấu hình Chatbox AI: bật/tắt, chọn model, chỉnh System Prompt. Lưu trong database qua IAiSettingsService.
/// Quyền truy cập do AdminBaseController đảm bảo ([Authorize(Roles = ADMIN)]).
/// Khóa API KHÔNG cấu hình ở đây: chỉ nằm trong User Secrets / biến môi trường.
/// </summary>
public sealed class AiSettingsController : AdminBaseController
{
    private readonly IAiSettingsService _settings;
    private readonly IAuditLogService _auditLog;

    public AiSettingsController(IAiSettingsService settings, IAuditLogService auditLog)
    {
        _settings = settings;
        _auditLog = auditLog;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var current = await _settings.GetAsync(cancellationToken);
        var vm = new AiSettingsVm
        {
            Enabled = current.Enabled,
            ModelName = current.Model,
            SystemPrompt = current.SystemPrompt
        };
        FillDisplayFields(vm, current);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AiSettingsVm vm, CancellationToken cancellationToken)
    {
        if (!_settings.AllowedModels.Contains(vm.ModelName, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(vm.ModelName), "Model AI không hợp lệ.");

        if (ModelState.IsValid)
        {
            int? adminId = int.TryParse(User.GetUserId(), out var id) ? id : null;
            var (success, error) = await _settings.SaveAsync(vm.Enabled, vm.ModelName, vm.SystemPrompt, adminId, cancellationToken);
            if (success)
            {
                if (adminId.HasValue)
                {
                    await _auditLog.LogAsync(adminId.Value, "UPDATE_AI_SETTINGS", "CauHinhAI", 1, "SUCCESS", $"Cập nhật cấu hình AI: Enabled={vm.Enabled}, Model={vm.ModelName}");
                }
                this.ThongBaoThanhCong("Đã lưu cấu hình Chatbox AI. Cấu hình mới có hiệu lực ngay cho các tin nhắn tiếp theo.");
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, error ?? "Không lưu được cấu hình.");
        }

        FillDisplayFields(vm, await _settings.GetAsync(cancellationToken));
        return View(nameof(Index), vm);
    }

    private void FillDisplayFields(AiSettingsVm vm, AiSettings current)
    {
        vm.AllowedModels = _settings.AllowedModels;
        vm.IsPersisted = current.IsPersisted;
        vm.UpdatedAtUtc = current.UpdatedAtUtc;
    }
}
