namespace SecondHandMarketplace.Web.Services.Ai;

public interface IAiChatService
{
    /// <param name="role">USER / ADMIN: quyết định AI được phép làm gì.</param>
    Task<string> AskAsync(string userId, string role, string message);
}
