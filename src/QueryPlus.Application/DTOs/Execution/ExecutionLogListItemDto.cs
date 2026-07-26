namespace QueryPlus.Application.DTOs.Execution;

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
