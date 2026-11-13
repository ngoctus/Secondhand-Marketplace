namespace SecondHandMarketplace.Web.Common;

/// <summary>
/// Kiểu danh sách phân trang chuẩn dùng chung toàn hệ thống.
/// Đặt trong thư mục Common theo đúng quy ước kiến trúc API Contract.
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalItems { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)Math.Max(1, PageSize));
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PagedResult() { }

    public PagedResult(IReadOnlyList<T> items, int totalItems, int page, int pageSize)
    {
        Items = items ?? Array.Empty<T>();
        TotalItems = totalItems;
        Page = page < 1 ? 1 : page;
        PageSize = pageSize < 1 ? 10 : pageSize;
    }

    public static PagedResult<T> Empty(int page = 1, int pageSize = 10) =>
        new(Array.Empty<T>(), 0, page, pageSize);
}
