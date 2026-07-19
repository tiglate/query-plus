namespace QueryPlus.Application.DTOs.Common;

public sealed class PagedResult<T>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public required IReadOnlyList<T> Items { get; init; }
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    /// <summary>
    /// Normalizes page/pageSize and optionally clamps page to the last available page when total is known.
    /// </summary>
    public static (int Page, int PageSize) Normalize(int page, int pageSize, int? totalCount = null)
    {
        var size = pageSize < 1
            ? DefaultPageSize
            : pageSize > MaxPageSize
                ? MaxPageSize
                : pageSize;

        var p = page < 1 ? 1 : page;

        if (totalCount is not null && size > 0)
        {
            var totalPages = totalCount.Value == 0
                ? 1
                : (int)Math.Ceiling(totalCount.Value / (double)size);
            if (p > totalPages)
            {
                p = totalPages;
            }
        }

        return (p, size);
    }
}
