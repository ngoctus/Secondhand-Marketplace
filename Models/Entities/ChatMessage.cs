namespace SecondHandMarketplace.Web.Models.Entities;

public class ChatMessage
{
    public int Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public AppUser Sender { get; set; } = null!;
    public string ReceiverId { get; set; } = string.Empty;
    public AppUser Receiver { get; set; } = null!;
    public int? ProductId { get; set; }      // trao đổi về sản phẩm nào (không bắt buộc)
    public int? OrderId { get; set; }        // hoặc về đơn nào (không bắt buộc)
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
