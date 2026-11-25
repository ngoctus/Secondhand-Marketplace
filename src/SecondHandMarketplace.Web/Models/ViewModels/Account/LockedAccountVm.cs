using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SecondHandMarketplace.Web.Services.Accounts;

namespace SecondHandMarketplace.Web.Models.ViewModels.Account;

/// <summary>
/// ViewModel cho trang /Account/Locked hiển thị chi tiết tài khoản bị khóa và form gửi khiếu nại / mở khóa (KN-07).
/// </summary>
public sealed class LockedAccountVm
{
    public string? Token { get; set; }
    public bool RequiresTokenVerification { get; set; }
    public LockedAccountOverviewDto? Overview { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại yêu cầu.")]
    public string LoaiYeuCau { get; set; } = AccountAppealRules.TypeAppeal;

    [Required(ErrorMessage = "Vui lòng nhập nội dung giải trình hoặc cam kết.")]
    [MinLength(20, ErrorMessage = "Nội dung giải trình / cam kết phải có ít nhất 20 ký tự.")]
    [MaxLength(2000, ErrorMessage = "Nội dung không được vượt quá 2000 ký tự.")]
    public string NoiDung { get; set; } = string.Empty;

    public IFormFile? FileDinhKem { get; set; }
}

/// <summary>
/// Model nhận dữ liệu POST gửi đơn từ trang /Account/Locked.
/// </summary>
public sealed class SubmitAppealFormVm
{
    public string? Token { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại yêu cầu.")]
    public string LoaiYeuCau { get; set; } = AccountAppealRules.TypeAppeal;

    [Required(ErrorMessage = "Vui lòng nhập nội dung giải trình hoặc cam kết.")]
    [MinLength(20, ErrorMessage = "Nội dung giải trình / cam kết phải có ít nhất 20 ký tự.")]
    [MaxLength(2000, ErrorMessage = "Nội dung không được vượt quá 2000 ký tự.")]
    public string NoiDung { get; set; } = string.Empty;

    public IFormFile? FileDinhKem { get; set; }
}
