using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.DTOs.Jobs;

/// <summary>See <see cref="CreateJobDefinitionDto"/> remarks on excluded fields.</summary>
public sealed class UpdateJobDefinitionDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public JobType JobType { get; init; }
    public required string CronExpression { get; init; }
    public required string RunAsUser { get; init; }
    public int MemoryLimitMb { get; init; }
    public int MaxDurationMinutes { get; init; }
    public string? NotifyEmails { get; init; }
}
