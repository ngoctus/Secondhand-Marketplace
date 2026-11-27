using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;

namespace SecondHandMarketplace.Web.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class SeoController(
    AppDbContext db,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<SeoController> logger) : Controller
{
    private static readonly XNamespace SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public ContentResult Robots()
    {
        var publicBaseUrl = GetPublicBaseUrl();
        var content = environment.IsDevelopment() || publicBaseUrl is null
            ? "User-agent: *\nDisallow: /\n"
            : "User-agent: *\nAllow: /\nDisallow: /Admin/\nDisallow: /User/\nDisallow: /Account/\nDisallow: /api/\nDisallow: /health/\n" +
              $"Sitemap: {publicBaseUrl}/sitemap.xml\n";

        return Content(content, "text/plain; charset=utf-8");
    }

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 900, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        var publicBaseUrl = GetPublicBaseUrl();
        if (publicBaseUrl is null)
            return Problem("Set Site:PublicBaseUrl to the public HTTPS origin before publishing a sitemap.", statusCode: StatusCodes.Status503ServiceUnavailable);

        var baseUri = new Uri(publicBaseUrl, UriKind.Absolute);
        var publicPages = new[]
        {
            "/", "/Products", "/PublicAuctions", "/gioi-thieu", "/faq", "/dieu-khoan",
            "/chinh-sach-mua-hang", "/chinh-sach-nguoi-ban", "/chinh-sach-bao-mat"
        };
        var entries = publicPages.Select(path => new XElement(SitemapNamespace + "url",
            new XElement(SitemapNamespace + "loc", new Uri(baseUri, path).AbsoluteUri))).ToList();

        try
        {
            var activeProductIds = await db.SanPhams.AsNoTracking()
                .Where(product => product.TrangThai == "ACTIVE" && !product.DaXoa && product.CuaHang.TrangThai == "ACTIVE")
                .OrderByDescending(product => product.NgayCapNhat)
                .Select(product => product.MaSanPham)
                .Take(10000)
                .ToListAsync(cancellationToken);

            foreach (var productId in activeProductIds)
            {
                var path = Url.Action("Details", "Products", new { area = "", id = productId });
                if (!string.IsNullOrWhiteSpace(path))
                    entries.Add(new XElement(SitemapNamespace + "url",
                        new XElement(SitemapNamespace + "loc", new Uri(baseUri, path).AbsoluteUri)));
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not add active product pages to sitemap; serving static public pages.");
        }

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(SitemapNamespace + "urlset", entries));
        return Content(document.ToString(SaveOptions.DisableFormatting), "application/xml; charset=utf-8");
    }

    private string? GetPublicBaseUrl()
    {
        var configured = configuration["Site:PublicBaseUrl"];
        return Uri.TryCreate(configured, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && string.IsNullOrEmpty(uri.UserInfo)
            && uri.AbsolutePath == "/"
            ? uri.GetLeftPart(UriPartial.Authority).TrimEnd('/')
            : null;
    }
}
