namespace QueryPlus.Application.DTOs.Execution;

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
