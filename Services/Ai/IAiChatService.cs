namespace Web2Hand.Services.Ai;

public interface IAiChatService
{
    /// <param name="role">Buyer / Seller / Admin: quyết định AI được phép làm gì.</param>
    Task<string> AskAsync(string userId, string role, string message);
}
