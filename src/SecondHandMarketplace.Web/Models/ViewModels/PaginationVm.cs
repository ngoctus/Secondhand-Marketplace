namespace SecondHandMarketplace.Web.Models.ViewModels;

/// <summary>
/// ViewModel dùng chung cho thành phần phân trang (_Pagination.cshtml).
/// </summary>
public class PaginationVm
{
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; } = 0;
    public string Action { get; set; } = "Index";
    public string? Controller { get; set; }
    public string? Area { get; set; }
    public Dictionary<string, string?> RouteValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
