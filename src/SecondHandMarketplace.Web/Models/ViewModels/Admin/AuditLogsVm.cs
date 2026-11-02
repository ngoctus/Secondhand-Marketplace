namespace SecondHandMarketplace.Web.Models.ViewModels.Admin;

public sealed class AuditLogsVm
{
    public IReadOnlyList<AuditLogRowVm> Items { get; set; } = [];
    public string? UserId { get; set; }
    public string? Action { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
}

public sealed class AuditLogRowVm
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty;
    public int? ObjectId { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? Detail { get; set; }
    public DateTime Time { get; set; }
}
