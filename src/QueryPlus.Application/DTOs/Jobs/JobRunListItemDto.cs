using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.DTOs.Jobs;

public sealed class JobRunListItemDto
{
    public int Id { get; init; }
    public int JobDefinitionId { get; init; }
    public JobRunStatus Status { get; init; }
    public JobTriggerSource TriggeredBy { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? FinishedAt { get; init; }
    public int? ExitCode { get; init; }
    public string? HostMachine { get; init; }
}
