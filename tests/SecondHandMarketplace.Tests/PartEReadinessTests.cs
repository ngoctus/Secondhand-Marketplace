using System.Reflection;
using Microsoft.AspNetCore.RateLimiting;
using SecondHandMarketplace.Web.Controllers;
using SecondHandMarketplace.Web.Controllers.Api;

namespace SecondHandMarketplace.Tests;

public sealed class PartEReadinessTests
{
    [Fact]
    public void DemoSeed_IsSeparatedFromSystemSeed_AndCannotBeAppliedTwice()
    {
        var root = FindRepositoryRoot();
        var systemSeed = File.ReadAllText(Path.Combine(root, "database", "09_seed_data.sql"));
        var demoSeed = File.ReadAllText(Path.Combine(root, "database", "29_seed_demo_goc.sql"));

        Assert.DoesNotContain("INSERT NguoiDung", systemSeed, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INSERT NguoiDung", demoSeed, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Seed demo đã có dữ liệu", demoSeed, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EveryLoginPost_IsRateLimited()
    {
        AssertLoginIsLimited(typeof(AccountController));
        AssertLoginIsLimited(typeof(AuthController));
        AssertLoginIsLimited(typeof(SecondHandMarketplace.Web.Controllers.Api.v1.AuthController));
    }

    [Fact]
    public void AuctionLinks_AreAbsentFromNavigationAndAdminDashboard()
    {
        var root = FindRepositoryRoot();
        var layout = File.ReadAllText(Path.Combine(root, "src", "SecondHandMarketplace.Web", "Views", "Shared", "_Layout.cshtml"));
        var adminLinks = File.ReadAllText(Path.Combine(root, "src", "SecondHandMarketplace.Web", "Views", "Shared", "_AdminLinks.cshtml"));
        var dashboard = File.ReadAllText(Path.Combine(root, "src", "SecondHandMarketplace.Web", "Areas", "Admin", "Views", "Dashboard", "Index.cshtml"));

        Assert.DoesNotContain("PublicAuctions", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-controller=\"Auctions\"", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("Auctions", adminLinks, StringComparison.Ordinal);
        Assert.DoesNotContain("Auctions", dashboard, StringComparison.Ordinal);
        Assert.Contains("AiSettings", adminLinks, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicProductDto_DoesNotExposeShopPhone()
    {
        var propertyNames = typeof(SecondHandMarketplace.Web.Models.DTOs.Products.ProductDetailResponse)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name);
        Assert.DoesNotContain("ShopContactPhone", propertyNames);
    }

    private static void AssertLoginIsLimited(Type controller)
    {
        var action = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(m => m.Name == "Login" && m.GetParameters().Last().ParameterType == typeof(CancellationToken));
        Assert.NotNull(action);
        Assert.NotNull(action!.GetCustomAttribute<EnableRateLimitingAttribute>());
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "database", "09_seed_data.sql")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
