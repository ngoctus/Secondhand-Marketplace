using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace SecondHandMarketplace.Tests;

internal sealed class TestWebHostEnvironment(string contentRoot) : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "SecondHandMarketplace.Tests";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = Path.Combine(contentRoot, "wwwroot");
    public string EnvironmentName { get; set; } = "Testing";
    public string ContentRootPath { get; set; } = contentRoot;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
