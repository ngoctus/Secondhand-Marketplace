using Web2Hand.Models.Enums;

namespace Web2Hand.Models.Entities;

public class Report
{
    public int Id { get; set; }
    public string ReporterId { get; set; } = string.Empty;
    public AppUser Reporter { get; set; } = null!;
    public ReportTargetType TargetType { get; set; }
    public string TargetId { get; set; } = string.Empty;   // Id sản phẩm hoặc Id seller
    public string Reason { get; set; } = string.Empty;
    public ReportStatus Status { get; set; } = ReportStatus.New;
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
