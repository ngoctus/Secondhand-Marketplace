namespace SecondHandMarketplace.Web.Models.ViewModels.Chat;

public sealed class ConversationVm
{
    public int Id { get; set; }
    public string Title { get; set; } = "";       // tên người bên kia
    public string? Shop { get; set; }              // tên shop của người bên kia (nếu có)
    public string? LastMessage { get; set; }
    public string LastAtUtc { get; set; } = "";
    public int Unread { get; set; }
}

public sealed class ChatMessageVm
{
    public long Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string? SenderName { get; set; }        // chỉ có khi đẩy qua hub, dùng cho toast
    public string Content { get; set; } = "";
    public string SentAtUtc { get; set; } = "";
    public bool Read { get; set; }
}