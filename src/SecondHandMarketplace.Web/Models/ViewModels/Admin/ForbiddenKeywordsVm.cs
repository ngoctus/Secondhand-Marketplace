using SecondHandMarketplace.Web.Services.Moderation;

namespace SecondHandMarketplace.Web.Models.ViewModels.Admin;

public sealed class ForbiddenKeywordsVm
{
    public IReadOnlyList<ForbiddenKeywordItem> Items { get; set; } = Array.Empty<ForbiddenKeywordItem>();
    public string? Search { get; set; }
    public string? Level { get; set; }
    public bool? Active { get; set; }
}
