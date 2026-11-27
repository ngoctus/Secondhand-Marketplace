using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.ViewModels.Ai;
using SecondHandMarketplace.Web.Services.Ai;
using SecondHandMarketplace.Web.Services.AuditLogs;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

/// <summary>
/// Admin cấu hình 2 cụm AI trong database (dbo.CauHinhAI) qua IAiSettingsService:
/// - Cụm 1: AI Hỗ trợ người mua &amp; người bán (Chatbox tìm kiếm + Trợ lý bán hàng: mô tả &amp; tham khảo giá).
/// - Cụm 2: AI Kiểm duyệt sản phẩm Tầng 2 (chạy nội bộ).
/// Quyền truy cập do AdminBaseController đảm bảo ([Authorize(Roles = ADMIN)]).
/// Khóa API KHÔNG cấu hình ở đây: chỉ nằm trong User Secrets / biến môi trường (chỉ hiển thị trạng thái đã nạp hay chưa).
/// </summary>
public sealed class AiSettingsController : AdminBaseController
{
    private readonly IAiSettingsService _settings;
    private readonly IAuditLogService _auditLog;
    private readonly AiOptions _options;

    public AiSettingsController(IAiSettingsService settings, IAuditLogService auditLog, IConfiguration configuration)
    {
        _settings = settings;
        _auditLog = auditLog;
        _options = AiOptions.FromConfiguration(configuration);
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var current = await _settings.GetAsync(cancellationToken);
        var vm = new AiSettingsVm
        {
            Enabled = current.Enabled,
            SellerAssistantEnabled = current.SellerAssistantEnabled,
            ModelName = current.Model,
            SystemPrompt = current.SystemPrompt,
            ModerationEnabled = current.ModerationEnabled,
            ModerationModelName = current.EffectiveModerationModel,
            ModerationMaxImages = current.ModerationMaxImages
        };
        FillDisplayFields(vm, current);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AiSettingsVm vm, CancellationToken cancellationToken)
    {
        if (!_settings.AllowedModels.Contains(vm.ModelName, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(vm.ModelName), "Model AI hỗ trợ người mua & người bán không hợp lệ.");

        if (!_settings.AllowedModels.Contains(vm.ModerationModelName, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(vm.ModerationModelName), "Model AI kiểm duyệt không hợp lệ.");

        if (ModelState.IsValid)
        {
            var adminId = User.LayNguoiDungId();
            var update = new AiSettingsUpdate(
                Enabled: vm.Enabled,
                SellerAssistantEnabled: vm.SellerAssistantEnabled,
                Model: vm.ModelName,
                SystemPrompt: vm.SystemPrompt,
                ModerationEnabled: vm.ModerationEnabled,
                ModerationModel: vm.ModerationModelName,
                ModerationMaxImages: vm.ModerationMaxImages);

            var (success, error) = await _settings.SaveAsync(update, adminId, cancellationToken);
            if (success)
            {
                if (adminId.HasValue)
                {
                    await _auditLog.LogAsync(
                        adminId.Value,
                        "UPDATE_AI_SETTINGS",
                        "CauHinhAI",
                        1,
                        "SUCCESS",
                        $"Cập nhật cấu hình AI: Chat={vm.Enabled}, SellerAi={vm.SellerAssistantEnabled}, Model={vm.ModelName}; Moderation={vm.ModerationEnabled}, ModModel={vm.ModerationModelName}, ModImages={vm.ModerationMaxImages}");
                }
                this.ThongBaoThanhCong("Đã lưu cấu hình AI. Thay đổi có hiệu lực ngay.");
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
        vm.HasExtendedSchema = current.HasExtendedSchema;
        vm.UpdatedAtUtc = current.UpdatedAtUtc;
        vm.HasChatApiKey = !string.IsNullOrWhiteSpace(_options.ApiKey);
        vm.HasDedicatedModerationApiKey = !string.IsNullOrWhiteSpace(_options.ModerationApiKey);
        vm.HasFallbackApiKey = !string.IsNullOrWhiteSpace(_options.FallbackApiKey);
    }
}
