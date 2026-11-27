using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Hosting;
using SecondHandMarketplace.Web.Common;

namespace SecondHandMarketplace.Web.Services.Storage;

public sealed class UploadStorage(
    IWebHostEnvironment environment,
    IConfiguration configuration,
    ILogger<UploadStorage> logger,
    IAmazonS3? s3Client = null) : IUploadStorage
{
    private const long MaxReadBytes = 10 * 1024 * 1024;
    private readonly string _publicRoot = UploadStoragePaths.PublicRoot(environment, configuration);
    private readonly string _privateRoot = UploadStoragePaths.PrivateRoot(environment, configuration);
    private readonly string _bucket = configuration["Storage:S3:Bucket"]?.Trim() ?? string.Empty;

    public bool UsesObjectStorage { get; } = string.Equals(
        configuration["Storage:Provider"], "S3", StringComparison.OrdinalIgnoreCase);

    public string? PublicBaseUrl { get; } = NormalizeBaseUrl(configuration["Storage:S3:PublicBaseUrl"]);

    private readonly IAmazonS3? _s3Client = s3Client;

    public async Task<string> SaveAsync(Stream content, string relativeUploadPath, bool isPrivate, string contentType,
        CancellationToken cancellationToken = default)
    {
        var safePath = NormalizeUploadPath(relativeUploadPath);
        if (UsesObjectStorage)
        {
            var request = new PutObjectRequest
            {
                BucketName = RequireBucket(),
                Key = ToObjectKey(safePath, isPrivate),
                InputStream = content,
                ContentType = contentType
            };
            request.Headers.CacheControl = isPrivate ? "private, no-store" : "public, max-age=31536000, immutable";
            await RequireS3().PutObjectAsync(request, cancellationToken);
            return safePath;
        }

        var root = isPrivate ? _privateRoot : _publicRoot;
        var targetPath = ResolveLocalPath(safePath, root);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        await using var output = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None,
            81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await content.CopyToAsync(output, cancellationToken);
        return safePath;
    }

    public async Task<byte[]?> ReadAsync(string relativeUploadPath, bool isPrivate, CancellationToken cancellationToken = default)
    {
        var safePath = NormalizeUploadPath(relativeUploadPath);
        if (UsesObjectStorage)
        {
            try
            {
                using var response = await RequireS3().GetObjectAsync(RequireBucket(), ToObjectKey(safePath, isPrivate), cancellationToken);
                if (response.ContentLength is <= 0 or > MaxReadBytes) return null;
                return await ReadBoundedAsync(response.ResponseStream, response.ContentLength, cancellationToken);
            }
            catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Continue to the local fallback so an existing installation can be migrated gradually.
            }
        }

        var localPath = ResolveLocalPath(safePath, isPrivate ? _privateRoot : _publicRoot);
        if (!File.Exists(localPath)) return null;
        var info = new FileInfo(localPath);
        if (info.Length is <= 0 or > MaxReadBytes) return null;
        await using var input = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await ReadBoundedAsync(input, input.Length, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string relativeUploadPath, bool isPrivate, CancellationToken cancellationToken = default)
    {
        var safePath = NormalizeUploadPath(relativeUploadPath);
        if (UsesObjectStorage)
        {
            try
            {
                await RequireS3().GetObjectMetadataAsync(RequireBucket(), ToObjectKey(safePath, isPrivate), cancellationToken);
                return true;
            }
            catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Local fallback for assets uploaded before switching providers.
            }
        }
        return File.Exists(ResolveLocalPath(safePath, isPrivate ? _privateRoot : _publicRoot));
    }

    public async Task DeleteAsync(string relativeUploadPath, bool isPrivate, CancellationToken cancellationToken = default)
    {
        var safePath = NormalizeUploadPath(relativeUploadPath);
        try
        {
            if (UsesObjectStorage)
                await RequireS3().DeleteObjectAsync(RequireBucket(), ToObjectKey(safePath, isPrivate), cancellationToken);

            var localPath = ResolveLocalPath(safePath, isPrivate ? _privateRoot : _publicRoot);
            if (File.Exists(localPath)) File.Delete(localPath);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not delete uploaded object {UploadPath} from storage.", safePath);
        }
    }

    private string RequireBucket() => !string.IsNullOrWhiteSpace(_bucket)
        ? _bucket
        : throw new InvalidOperationException("Storage:S3:Bucket is required when Storage:Provider is S3.");

    private IAmazonS3 RequireS3() => _s3Client
        ?? throw new InvalidOperationException("An S3 client is required when Storage:Provider is S3.");

    private static async Task<byte[]?> ReadBoundedAsync(Stream input, long? expectedLength, CancellationToken cancellationToken)
    {
        var capacity = expectedLength is > 0 and <= MaxReadBytes ? (int)expectedLength.Value : 0;
        await using var output = capacity > 0 ? new MemoryStream(capacity) : new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = await input.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0) break;
            if (output.Length + read > MaxReadBytes) return null;
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return output.Length == 0 ? null : output.ToArray();
    }

    private static string ToObjectKey(string relativePath, bool isPrivate)
    {
        var key = relativePath["/uploads/".Length..];
        return isPrivate ? $"private/{key}" : key;
    }

    private static string NormalizeUploadPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Upload path is required.", nameof(path));
        var normalized = path.Replace('\\', '/').Trim();
        if (!normalized.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
            || normalized.Split('/').Any(segment => segment is "." or ".."))
            throw new ArgumentException("Upload path must stay below /uploads/.", nameof(path));
        return normalized;
    }

    private static string ResolveLocalPath(string relativePath, string root)
    {
        var relative = relativePath["/uploads/".Length..].Replace('/', Path.DirectorySeparatorChar);
        return UploadStoragePaths.ResolveLocalUpload($"/uploads/{relative.Replace(Path.DirectorySeparatorChar, '/')}", root)
            ?? throw new InvalidOperationException("Upload path resolved outside the configured storage root.");
    }

    private static string? NormalizeBaseUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim().TrimEnd('/');
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("https" or "http"))
            throw new InvalidOperationException("Storage:S3:PublicBaseUrl must be an absolute HTTP(S) URL.");
        return normalized;
    }
}
