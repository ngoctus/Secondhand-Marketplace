namespace SecondHandMarketplace.Web.Models.Entities;

public class Favorite
{
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
