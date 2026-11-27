namespace SecondHandMarketplace.Web.Services.Storage;

public interface IUploadStorage
{
    bool UsesObjectStorage { get; }
    string? PublicBaseUrl { get; }

    Task<string> SaveAsync(Stream content, string relativeUploadPath, bool isPrivate, string contentType,
        CancellationToken cancellationToken = default);
    Task<byte[]?> ReadAsync(string relativeUploadPath, bool isPrivate, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string relativeUploadPath, bool isPrivate, CancellationToken cancellationToken = default);
    Task DeleteAsync(string relativeUploadPath, bool isPrivate, CancellationToken cancellationToken = default);
}
