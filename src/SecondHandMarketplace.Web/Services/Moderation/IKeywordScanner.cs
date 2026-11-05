namespace SecondHandMarketplace.Web.Services.Moderation;

/// <summary>Quét tên và mô tả sản phẩm tìm từ khóa cấm đang áp dụng. Không gọi dịch vụ AI bên ngoài.</summary>
public interface IKeywordScanner
{
    Task<IReadOnlyList<KeywordMatch>> ScanAsync(string? name, string? description, CancellationToken cancellationToken = default);
}
