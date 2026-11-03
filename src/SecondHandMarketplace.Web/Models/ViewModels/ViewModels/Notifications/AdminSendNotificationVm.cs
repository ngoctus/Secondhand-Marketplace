using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.ViewModels.Notifications;

public sealed class AdminSendNotificationVm
{
    public const string TargetUser = "USER";
    public const string TargetAll = "ALL";

    /// <summary>USER = một người dùng cụ thể (theo email); ALL = toàn bộ tài khoản đang hoạt động.</summary>
    [Required(ErrorMessage = "Vui lòng chọn đối tượng nhận.")]
    public string Target { get; set; } = TargetUser;

    [Display(Name = "Email người nhận")]
    [StringLength(256)]
    public string? RecipientEmail { get; set; }

    [Display(Name = "Tiêu đề")]
    [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
    [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
    public string TieuDe { get; set; } = string.Empty;

    [Display(Name = "Nội dung")]
    [Required(ErrorMessage = "Nội dung là bắt buộc.")]
    [StringLength(1000, ErrorMessage = "Nội dung tối đa 1000 ký tự.")]
    public string NoiDung { get; set; } = string.Empty;
}
