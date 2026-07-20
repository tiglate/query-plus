namespace QueryPlus.Application.Common;

/// <summary>
/// Standard server-side pagination contract for heavy stored procedures.
/// Reserved parameter names are never user-facing catalog fields.
/// </summary>
public static class ProcedurePagination
{
    public const string PageNumberName = "@PageNumber";
    public const string PageSizeName = "@PageSize";
    public const string TotalRecordsName = "@TotalRecords";

    public const long DefaultPageNumber = 1;
    public const long DefaultPageSize = 50;
    /// <summary>Max page size for interactive UI execute (not export).</summary>
    public const long MaxUiPageSize = 200;
    /// <summary>Export uses a single giant page as product convention.</summary>
    public const long ExportPageSize = 999_999_999L;

    /// <summary>ADO.NET / SP command timeout (30 minutes).</summary>
    public const int CommandTimeoutSeconds = 1800;

    private static readonly HashSet<string> ReservedBareNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "PageNumber",
        "PageSize",
        "TotalRecords"
    };

    public static bool IsReservedParameterName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var bare = name.Trim().TrimStart('@');
        return ReservedBareNames.Contains(bare);
    }

    public static string NormalizeReservedName(string bareName)
        => bareName.StartsWith('@') ? bareName : $"@{bareName}";

    public static long ClampPageNumber(long? pageNumber)
    {
        if (pageNumber is null or < 1)
        {
            return DefaultPageNumber;
        }

        return pageNumber.Value;
    }

    public static long ClampUiPageSize(long? pageSize)
    {
        if (pageSize is null or < 1)
        {
            return DefaultPageSize;
        }

        return pageSize.Value > MaxUiPageSize ? MaxUiPageSize : pageSize.Value;
    }

    /// <summary>
    /// Injects paging inputs into a bound parameter dictionary (mutates a copy).
    /// </summary>
    public static Dictionary<string, object?> WithPagingInputs(
        IReadOnlyDictionary<string, object?> userParameters,
        long pageNumber,
        long pageSize)
    {
        var result = new Dictionary<string, object?>(userParameters, StringComparer.OrdinalIgnoreCase)
        {
            [PageNumberName] = pageNumber,
            [PageSizeName] = pageSize
        };
        return result;
    }
}
