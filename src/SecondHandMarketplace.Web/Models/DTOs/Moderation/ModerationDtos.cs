using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.DTOs.Moderation;

public class ModerationDecisionRequest
{
    [Required]
    public bool Approved { get; set; }

    [StringLength(500, ErrorMessage = "Lý do từ chối không quá 500 ký tự.")]
    public string? Reason { get; set; }
}

public class PendingProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? CategoryName { get; set; }
    public string? ConditionName { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? ModerationNotes { get; set; }
}

public class ModerationHistoryResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty; // APPROVED, REJECTED, AUTO_REJECTED
    public string? Reason { get; set; }
    public string ModeratedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
