using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.DTOs.Jobs;

public sealed class JobDefinitionDetailDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public JobType JobType { get; init; }
    public string? ScriptPath { get; init; }
    public string? ScriptSha256 { get; init; }
    public required string CronExpression { get; init; }
    public required string RunAsUser { get; init; }
    public int MemoryLimitMb { get; init; }
    public int MaxDurationMinutes { get; init; }
    public bool Enabled { get; init; }
    public JobApprovalStatus ApprovalStatus { get; init; }
    public required string CreatedBy { get; init; }
    public string? ApprovedBy { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public string? RejectionReason { get; init; }
    public string? NotifyEmails { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
