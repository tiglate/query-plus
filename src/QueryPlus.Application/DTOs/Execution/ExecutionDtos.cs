using System.Data;
using QueryPlus.Domain.Enums;

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

public sealed class ExecutionResultDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int? ExecutionLogId { get; init; }
    public int ProcedureId { get; init; }
    public string? ProcedureCaption { get; init; }
    /// <summary>Rows in the current result page (or full set if not paginated).</summary>
    public int RowCount { get; init; }

    /// <summary>True when the procedure uses server-side pagination.</summary>
    public bool SupportsPagination { get; init; }

    /// <summary>Current page number (paginated only).</summary>
    public long PageNumber { get; init; } = 1;

    /// <summary>Page size used for this execute (paginated only).</summary>
    public long PageSize { get; init; }

    /// <summary>Total rows across all pages (@TotalRecords OUTPUT).</summary>
    public long? TotalRecords { get; init; }

    /// <summary>Raw tabular result for the grid (ADO.NET).</summary>
    public DataTable? Data { get; init; }

    /// <summary>Column metadata from configuration (captions, alignment, format).</summary>
    public IReadOnlyList<GridColumnDto> Columns { get; init; } = [];
}

public sealed class GridColumnDto
{
    public required string TechnicalName { get; init; }
    public required string Caption { get; init; }
    public ColumnAlignment Alignment { get; init; }
    public string? FormatMask { get; init; }
    public bool Visible { get; init; } = true;
}

public sealed class ExecutionLogDto
{
    public int Id { get; init; }
    public int ProcedureId { get; init; }
    public required string Username { get; init; }
    public string? IpAddress { get; init; }
    public DateTime ExecutionStart { get; init; }
    public DateTime? ExecutionEnd { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ParameterValuesJson { get; init; }
    public int? RowCount { get; init; }
}

public sealed class ExecutionLogListItemDto
{
    public int Id { get; init; }
    public int ProcedureId { get; init; }
    public required string ProcedureCaption { get; init; }
    public required string Username { get; init; }
    public string? IpAddress { get; init; }
    public DateTime ExecutionStart { get; init; }
    public DateTime? ExecutionEnd { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int? RowCount { get; init; }
}

public sealed class ExecutionLogFilterDto
{
    public string? Username { get; init; }
    public int? ProcedureId { get; init; }
    public bool? Success { get; init; }

    /// <summary>Inclusive local calendar date (time component ignored).</summary>
    public DateTime? StartFrom { get; init; }

    /// <summary>Inclusive local calendar date (time component ignored).</summary>
    public DateTime? StartTo { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
