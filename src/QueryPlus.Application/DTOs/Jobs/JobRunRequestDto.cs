namespace QueryPlus.Application.DTOs.Jobs;

public sealed class JobRunRequestDto
{
    public int Id { get; init; }
    public int JobDefinitionId { get; init; }
    public required string RequestedBy { get; init; }
    public DateTime RequestedAt { get; init; }
    public DateTime? ConsumedAt { get; init; }
    public int? JobRunId { get; init; }
}
