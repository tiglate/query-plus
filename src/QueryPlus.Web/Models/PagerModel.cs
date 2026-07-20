namespace QueryPlus.Web.Models;

/// <summary>
/// View model for the shared admin grid pager partial.
/// </summary>
public sealed class PagerModel
{
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public required int TotalPages { get; init; }
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    /// <summary>MVC controller name for page links (e.g. "Categories").</summary>
    public required string Controller { get; init; }

    /// <summary>MVC action name for page links (default Index).</summary>
    public string Action { get; init; } = "Index";

    /// <summary>
    /// Extra query values preserved on page links (filters). Do not include Page.
    /// </summary>
    public IReadOnlyDictionary<string, string?> RouteValues { get; init; }
        = new Dictionary<string, string?>();

    public int FromItem => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int ToItem => Math.Min(Page * PageSize, TotalCount);

    public IEnumerable<int> VisiblePages(int window = 2)
    {
        if (TotalPages <= 0)
        {
            yield break;
        }

        var start = Math.Max(1, Page - window);
        var end = Math.Min(TotalPages, Page + window);

        for (var i = start; i <= end; i++)
        {
            yield return i;
        }
    }
}
