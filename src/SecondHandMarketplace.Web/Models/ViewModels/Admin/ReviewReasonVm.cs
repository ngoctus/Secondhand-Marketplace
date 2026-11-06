using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.ViewModels.Admin;

/// <summary>Lý do từ chối / gỡ sản phẩm (khớp sp_QuyetDinhKiemDuyet: 10-500 ký tự).</summary>
public sealed class ReviewReasonVm
{
    [Required(ErrorMessage = "Vui lòng nhập lý do.")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Lý do phải từ 10 đến 500 ký tự.")]
    public string? Reason { get; set; }
}
