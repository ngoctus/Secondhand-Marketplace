using SecondHandMarketplace.Web.Models.Enums;

namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Khiếu nại / tranh chấp về một đơn hàng.</summary>
public class Dispute
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string ComplainantId { get; set; } = string.Empty;
    public AppUser Complainant { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string? EvidenceUrl { get; set; }
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    public string? Resolution { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
