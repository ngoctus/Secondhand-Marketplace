namespace SecondHandMarketplace.Web.Services.Moderation;

public sealed record ForbiddenKeywordItem(
    int Id, string Phrase, string NormalizedPhrase, string? Group, string Level, string? Note, bool Active, DateTime CreatedAt);

/// <summary>Quản lý TuKhoaCam cho Admin (xem, thêm, bật/tắt, sửa nhóm/mức độ/ghi chú). Không có chức năng xóa.</summary>
public interface IForbiddenKeywordService
{
    /// <summary>level: LOW/HIGH hoặc null = tất cả; active: null = tất cả.</summary>
    Task<IReadOnlyList<ForbiddenKeywordItem>> ListAsync(string? search = null, string? level = null, bool? active = null,
        CancellationToken cancellationToken = default);

    /// <summary>Thêm qua sp_ThemTuKhoaCam (tự chuẩn hóa, chống trùng) rồi đặt mức độ. Từ khóa mới luôn đang áp dụng.</summary>
    Task<(bool Success, string? Error, int? Id)> AddAsync(string phrase, string? group, string level, string? note,
        int? actorId, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> SetActiveAsync(int id, bool active, int? actorId,
        CancellationToken cancellationToken = default);

    /// <summary>Sửa nhóm, mức độ, ghi chú (không đổi nội dung cụm từ). Chỉ ảnh hưởng các lần phân tích SAU; lượt đã phân tích giữ nguyên cho tới khi phân tích lại.</summary>
    Task<(bool Success, string? Error)> UpdateAsync(int id, string? group, string level, string? note, int? actorId,
        CancellationToken cancellationToken = default);
}
