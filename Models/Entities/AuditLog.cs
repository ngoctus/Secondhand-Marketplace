namespace Web2Hand.Models.Entities;

/// <summary>Admin đã làm gì, với đối tượng nào, lúc nào.</summary>
public class AuditLog
{
    public int Id { get; set; }
    public string AdminId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;       // VD: "LockUser", "ApproveProduct"
    public string TargetType { get; set; } = string.Empty;   // VD: "User", "Product"
    public string TargetId { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
