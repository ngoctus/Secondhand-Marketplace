namespace SecondHandMarketplace.Web.Services.Storage;

public sealed class PublicObjectStorageMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUploadStorage storage)
    {
        if (!storage.UsesObjectStorage || !(HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (!(path.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase)
              || path.StartsWith("/uploads/banners/", StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        if (storage.PublicBaseUrl is { Length: > 0 } publicBaseUrl)
        {
            context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            context.Response.Redirect($"{publicBaseUrl}/{path["/uploads/".Length..]}", permanent: false);
            return;
        }

        var bytes = await storage.ReadAsync(path, isPrivate: false, context.RequestAborted);
        if (bytes is null && path.EndsWith("-thumb.webp", StringComparison.OrdinalIgnoreCase))
        {
            var basePath = path[..^"-thumb.webp".Length];
            foreach (var extension in new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" })
            {
                bytes = await storage.ReadAsync(basePath + extension, isPrivate: false, context.RequestAborted);
                if (bytes is not null) break;
            }
        }

        if (bytes is null)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = ContentType(path);
        context.Response.ContentLength = bytes.Length;
        context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        if (HttpMethods.IsGet(context.Request.Method))
            await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
    }

    private static string ContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        _ => "application/octet-stream"
    };
}
