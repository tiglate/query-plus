using System.Data;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.DTOs.Execution;

public sealed class ExecuteProcedureRequest
{
    public int ProcedureId { get; init; }

    /// <summary>
    /// Parameter values keyed by SQL parameter name (with or without leading @).
    /// </summary>
    public IDictionary<string, string?> ParameterValues { get; init; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}

public sealed class ExecutionResultDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int? ExecutionLogId { get; init; }
    public int ProcedureId { get; init; }
    public string? ProcedureCaption { get; init; }
    public int RowCount { get; init; }

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
