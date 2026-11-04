namespace SecondHandMarketplace.Web.Models.ViewModels;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalItems { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)Math.Max(1, PageSize));
}

public sealed record PagerVm(int Page, int TotalPages);