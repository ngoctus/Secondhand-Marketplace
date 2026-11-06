using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SecondHandMarketplace.Web.Models.ViewModels.Ai;

/// <summary>Form cấu hình Chatbox AI của Admin.</summary>
public sealed class AiSettingsVm
{
    [Display(Name = "Bật Chatbox AI")]
    public bool Enabled { get; set; }

    [Display(Name = "Model AI")]
    [Required(ErrorMessage = "Vui lòng chọn model AI.")]
    [StringLength(100)]
    public string ModelName { get; set; } = string.Empty;

    [Display(Name = "System Prompt")]
    [Required(ErrorMessage = "System Prompt không được để trống.")]
    [StringLength(4000, ErrorMessage = "System Prompt tối đa 4000 ký tự.")]
    public string SystemPrompt { get; set; } = string.Empty;

    // Chỉ để hiển thị, không nhận từ form.
    [BindNever] public IReadOnlyList<string> AllowedModels { get; set; } = Array.Empty<string>();
    [BindNever] public bool IsPersisted { get; set; }
    [BindNever] public DateTime? UpdatedAtUtc { get; set; }
}
