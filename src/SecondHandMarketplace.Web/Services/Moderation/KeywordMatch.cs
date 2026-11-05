namespace SecondHandMarketplace.Web.Services.Moderation;

/// <summary>
/// Một từ khóa cấm được tìm thấy. Field: "Ten" hoặc "MoTa". Position: chỉ số ký tự trong chuỗi ĐÃ CHUẨN HÓA
/// (chữ thường, bỏ dấu) của trường đó, null nếu không xác định được vị trí.
/// </summary>
public sealed record KeywordMatch(int KeywordId, string Phrase, string? Group, string Level, string Field, int? Position);
