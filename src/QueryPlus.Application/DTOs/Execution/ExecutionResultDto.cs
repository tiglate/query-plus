using System.Data;

namespace QueryPlus.Application.DTOs.Execution;

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
