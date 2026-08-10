using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.DTOs.Jobs;

public sealed class JobDefinitionListItemDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public JobType JobType { get; init; }
    public string? ScriptPath { get; init; }
    public JobApprovalStatus ApprovalStatus { get; init; }
    public bool Enabled { get; init; }
    public required string CronExpression { get; init; }
    public required string RunAsUser { get; init; }
    public required string CreatedBy { get; init; }
    public string? ApprovedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
