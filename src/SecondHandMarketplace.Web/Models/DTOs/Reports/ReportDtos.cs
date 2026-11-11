using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.DTOs.Reports;

public class CreateReportRequest
{
    [Required]
    public string TargetType { get; set; } = string.Empty; // PRODUCT, USER, REVIEW

    [Required]
    public int TargetId { get; set; }

    [Required(ErrorMessage = "Lý do báo cáo không được để trống.")]
    [StringLength(200)]
    public string Reason { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }
}

public class ReportResponse
{
    public int Id { get; set; }
    public int ReporterId { get; set; }
    public string ReporterName { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty; // PENDING, RESOLVED, DISMISSED
    public string? ResolutionNote { get; set; }
    public DateTime CreatedAt { get; set; }
}
