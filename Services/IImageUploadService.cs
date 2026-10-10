
using Microsoft.AspNetCore.Http;

namespace SecondHandMarketplace.Web.Services;

public interface IImageUploadService
{
    Task<ImageUploadResult> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string fileName);
}

public sealed record ImageUploadResult(
    bool Success,
    string? FileName,
    string? Url,
    string? Error);
