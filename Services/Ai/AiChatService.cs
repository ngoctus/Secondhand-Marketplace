namespace SecondHandMarketplace.Web.Services.Ai;

public class AiChatService : IAiChatService
{
    public Task<string> AskAsync(string userId, string role, string message)
    {
        // TODO: gọi API AI (Claude / Gemini / OpenAI) bằng HttpClient.
        // - Dùng tool/function calling: định nghĩa tool searchProducts, getAuctionInfo... để AI lấy DỮ LIỆU THẬT từ DB.
        // - USER: tìm / tư vấn sản phẩm, hỗ trợ đấu giá, viết mô tả sản phẩm. ADMIN: cấu hình AI.
        // - API key để trong User Secrets, KHÔNG commit lên Git.
        return Task.FromResult("Chatbox AI chưa được cài đặt.");
    }
}
