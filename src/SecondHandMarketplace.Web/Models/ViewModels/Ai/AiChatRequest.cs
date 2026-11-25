namespace SecondHandMarketplace.Web.Models.ViewModels.Ai;

/// <summary>Message cho phép null để controller tự kiểm tra và trả lỗi theo định dạng thống nhất (KetQua). Language tùy chọn ("vi" | "en").</summary>
public record AiChatRequest(string? Message, string? Language = null);
