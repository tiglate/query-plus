namespace QueryPlus.Application.DTOs.Execution;

public sealed class ExecuteProcedureRequest
{
    public int ProcedureId { get; init; }

    /// <summary>
    /// Parameter values keyed by SQL parameter name (with or without leading @).
    /// Must not include reserved pagination names.
    /// </summary>
    public IDictionary<string, string?> ParameterValues { get; init; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>1-based page when the procedure supports pagination.</summary>
    public long? PageNumber { get; init; }

    /// <summary>Page size when the procedure supports pagination (UI-capped).</summary>
    public long? PageSize { get; init; }
}
