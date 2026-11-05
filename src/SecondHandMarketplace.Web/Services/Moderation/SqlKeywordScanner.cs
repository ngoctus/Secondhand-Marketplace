using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;

namespace SecondHandMarketplace.Web.Services.Moderation;

/// <summary>
/// Dùng hàm SQL có sẵn dbo.fn_TimTuKhoaCam (khớp theo ranh giới từ trên chuỗi đã chuẩn hóa, chỉ từ khóa DangApDung = 1)
/// để quyết định có phát hiện hay không; sau đó lấy nhóm/mức độ từ TuKhoaCam. Vị trí là phần phụ, tính bằng C#.
/// </summary>
public sealed class SqlKeywordScanner(AppDbContext db) : IKeywordScanner
{
    public async Task<IReadOnlyList<KeywordMatch>> ScanAsync(string? name, string? description, CancellationToken cancellationToken = default)
    {
        var result = new List<KeywordMatch>();
        await ScanFieldAsync("Ten", name, result, cancellationToken);
        await ScanFieldAsync("MoTa", description, result, cancellationToken);
        return result;
    }

    private async Task ScanFieldAsync(string field, string? text, List<KeywordMatch> result, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var ids = await db.Database
            .SqlQuery<int>($"SELECT k.Id AS [Value] FROM dbo.fn_TimTuKhoaCam({text}) AS k")
            .ToListAsync(ct);
        if (ids.Count == 0) return;

        var keywords = await db.TuKhoaCams.AsNoTracking()
            .Where(k => ids.Contains(k.MaTuKhoaCam))
            .ToListAsync(ct);

        foreach (var k in keywords)
            result.Add(new KeywordMatch(k.MaTuKhoaCam, k.CumTu, k.Nhom, k.MucDo, field,
                ModerationDecision.FindPosition(text, k.CumTuChuanHoa)));
    }
}
