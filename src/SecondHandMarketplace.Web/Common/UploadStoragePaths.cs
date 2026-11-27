using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace SecondHandMarketplace.Web.Common;

public static class UploadStoragePaths
{
    public static string PublicRoot(IWebHostEnvironment environment, IConfiguration configuration) =>
        ResolveRoot(environment, configuration["Storage:RootPath"], Path.Combine("wwwroot", "uploads"));

    public static string PrivateRoot(IWebHostEnvironment environment, IConfiguration configuration) =>
        ResolveRoot(environment, configuration["Storage:PrivateRootPath"], Path.Combine("App_Data", "private-uploads"));

    public static void EnsurePrivateRootIsNotPublic(string publicRoot, string privateRoot)
    {
        var publicPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(publicRoot));
        var privatePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(privateRoot));
        if (privatePath.Equals(publicPath, StringComparison.OrdinalIgnoreCase)
            || privatePath.StartsWith(publicPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Storage:PrivateRootPath must not be the public upload root or a child of it.");
        }
    }

    public static string? ResolveLocalUpload(string? publicUrl, string root, string? expectedFolder = null)
    {
        const string prefix = "/uploads/";
        if (string.IsNullOrWhiteSpace(publicUrl) || !publicUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var relative = publicUrl[prefix.Length..].Split('?', '#')[0].Replace('/', Path.DirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) return null;
        if (expectedFolder is not null && !relative.StartsWith(expectedFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return null;

        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relative));
        return fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? fullPath
            : null;
    }

    private static string ResolveRoot(IWebHostEnvironment environment, string? configuredPath, string fallback)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath) ? fallback : configuredPath.Trim();
        return Path.GetFullPath(Path.IsPathRooted(path)
            ? path
            : Path.Combine(environment.ContentRootPath, path));
    }
}
