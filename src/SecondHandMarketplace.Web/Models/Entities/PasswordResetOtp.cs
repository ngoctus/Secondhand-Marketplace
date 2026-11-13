namespace SecondHandMarketplace.Web.Models.Entities;

public sealed class PasswordResetOtp
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string CodeProtected { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public int FailedAttempts { get; set; }
}
