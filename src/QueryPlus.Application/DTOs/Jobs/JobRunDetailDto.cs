using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.DTOs.Jobs;

public sealed class JobRunDetailDto
{
    public int Id { get; init; }
    public int JobDefinitionId { get; init; }
    public JobRunStatus Status { get; init; }
    public JobTriggerSource TriggeredBy { get; init; }
    public int? RunnerPid { get; init; }
    public DateTime? RunnerStartedAtUtc { get; init; }
    public int? ChildPid { get; init; }
    public DateTime? ChildStartedAtUtc { get; init; }
    public DateTime? LastHeartbeatUtc { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? FinishedAt { get; init; }
    public int? ExitCode { get; init; }
    public string? StdoutPath { get; init; }
    public string? StderrPath { get; init; }
    public string? HostMachine { get; init; }
    public DateTime CreatedAt { get; init; }
}
