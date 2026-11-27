using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SecondHandMarketplace.Web.Models.ViewModels.Ai;

/// <summary>
/// Form cấu hình hệ thống AI của Admin gồm 2 cụm:
/// 1. AI Hỗ trợ người mua &amp; người bán (Chatbox tìm kiếm + Trợ lý đăng bán: mô tả &amp; tham khảo giá).
/// 2. AI Kiểm duyệt sản phẩm Tầng 2 (chạy nội bộ).
/// </summary>
public sealed class AiSettingsVm
{
    // --- Cụm 1: AI Hỗ trợ người mua & người bán ---

    [Display(Name = "Bật Chatbox AI")]
    public bool Enabled { get; set; }

    [Display(Name = "Bật Trợ lý bán hàng (Viết mô tả & Tham khảo giá)")]
    public bool SellerAssistantEnabled { get; set; } = true;

    [Display(Name = "Model AI hỗ trợ người mua & người bán")]
    [Required(ErrorMessage = "Vui lòng chọn model AI.")]
    [StringLength(100)]
    public string ModelName { get; set; } = string.Empty;

    [Display(Name = "System Prompt (Chatbox AI)")]
    [Required(ErrorMessage = "System Prompt không được để trống.")]
    [StringLength(4000, ErrorMessage = "System Prompt tối đa 4000 ký tự.")]
    public string SystemPrompt { get; set; } = string.Empty;

    // --- Cụm 2: AI Kiểm duyệt sản phẩm (Tầng 2 Hybrid) ---

    [Display(Name = "Bật AI kiểm duyệt sản phẩm (Tầng 2)")]
    public bool ModerationEnabled { get; set; } = true;

    [Display(Name = "Model AI kiểm duyệt")]
    [Required(ErrorMessage = "Vui lòng chọn model AI kiểm duyệt.")]
    [StringLength(100)]
    public string ModerationModelName { get; set; } = string.Empty;

    [Display(Name = "Số ảnh đại diện tối đa gửi AI kiểm duyệt")]
    [Range(0, 4, ErrorMessage = "Số ảnh tối đa phải từ 0 đến 4.")]
    public int ModerationMaxImages { get; set; } = 2;

    // --- Chỉ để hiển thị trạng thái, không nhận từ form ---

    [BindNever] public IReadOnlyList<string> AllowedModels { get; set; } = Array.Empty<string>();
    [BindNever] public bool IsPersisted { get; set; }
    [BindNever] public bool HasExtendedSchema { get; set; }
    [BindNever] public DateTime? UpdatedAtUtc { get; set; }
    [BindNever] public bool HasChatApiKey { get; set; }
    [BindNever] public bool HasDedicatedModerationApiKey { get; set; }
    [BindNever] public bool HasFallbackApiKey { get; set; }
}
