using QueryPlus.Application.DTOs.Jobs;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Application.Mapping;

/// <summary>
/// Entity → DTO projections for <see cref="JobDefinition"/>, <see cref="JobRun"/> and
/// <see cref="JobRunRequest"/>.
/// </summary>
public static class JobMapper
{
    public static JobDefinitionListItemDto ToListItemDto(JobDefinition entity) => new()
    {
        Id = entity.IdJobDefinition,
        Name = entity.Name,
        JobType = entity.JobType,
        ScriptPath = entity.ScriptPath,
        ApprovalStatus = entity.ApprovalStatus,
        Enabled = entity.Enabled,
        CronExpression = entity.CronExpression,
        RunAsUser = entity.RunAsUser,
        CreatedBy = entity.CreatedBy,
        ApprovedBy = entity.ApprovedBy,
        UpdatedAt = entity.UpdatedAt,
    };

    public static IReadOnlyList<JobDefinitionListItemDto> ToListItemDtos(IEnumerable<JobDefinition> entities) =>
        entities.Select(ToListItemDto).ToArray();

    public static JobDefinitionDetailDto ToDetailDto(JobDefinition entity) => new()
    {
        Id = entity.IdJobDefinition,
        Name = entity.Name,
        Description = entity.Description,
        JobType = entity.JobType,
        ScriptPath = entity.ScriptPath,
        ScriptSha256 = entity.ScriptSha256,
        CronExpression = entity.CronExpression,
        RunAsUser = entity.RunAsUser,
        MemoryLimitMb = entity.MemoryLimitMb,
        MaxDurationMinutes = entity.MaxDurationMinutes,
        Enabled = entity.Enabled,
        ApprovalStatus = entity.ApprovalStatus,
        CreatedBy = entity.CreatedBy,
        ApprovedBy = entity.ApprovedBy,
        ApprovedAt = entity.ApprovedAt,
        RejectionReason = entity.RejectionReason,
        NotifyEmails = entity.NotifyEmails,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };

    public static JobRunListItemDto ToListItemDto(JobRun entity) => new()
    {
        Id = entity.IdJobRun,
        JobDefinitionId = entity.IdJobDefinition,
        Status = entity.Status,
        TriggeredBy = entity.TriggeredBy,
        StartedAt = entity.StartedAt,
        FinishedAt = entity.FinishedAt,
        ExitCode = entity.ExitCode,
        HostMachine = entity.HostMachine,
    };

    public static IReadOnlyList<JobRunListItemDto> ToListItemDtos(IEnumerable<JobRun> entities) =>
        entities.Select(ToListItemDto).ToArray();

    public static JobRunDetailDto ToDetailDto(JobRun entity) => new()
    {
        Id = entity.IdJobRun,
        JobDefinitionId = entity.IdJobDefinition,
        Status = entity.Status,
        TriggeredBy = entity.TriggeredBy,
        RunnerPid = entity.RunnerPid,
        RunnerStartedAtUtc = entity.RunnerStartedAtUtc,
        ChildPid = entity.ChildPid,
        ChildStartedAtUtc = entity.ChildStartedAtUtc,
        LastHeartbeatUtc = entity.LastHeartbeatUtc,
        StartedAt = entity.StartedAt,
        FinishedAt = entity.FinishedAt,
        ExitCode = entity.ExitCode,
        StdoutPath = entity.StdoutPath,
        StderrPath = entity.StderrPath,
        HostMachine = entity.HostMachine,
        CreatedAt = entity.CreatedAt,
    };

    public static JobRunRequestDto ToDto(JobRunRequest entity) => new()
    {
        Id = entity.IdJobRunRequest,
        JobDefinitionId = entity.IdJobDefinition,
        RequestedBy = entity.RequestedBy,
        RequestedAt = entity.RequestedAt,
        ConsumedAt = entity.ConsumedAt,
        JobRunId = entity.IdJobRun,
    };
}
