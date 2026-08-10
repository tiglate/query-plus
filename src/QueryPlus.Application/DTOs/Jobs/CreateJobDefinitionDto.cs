using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.DTOs.Jobs;

/// <summary>
/// Deliberately excludes Enabled/ApprovalStatus/ApprovedBy/ScriptSha256 - those only change via
/// dedicated service methods (SubmitForApprovalAsync/ApproveAsync/RejectAsync/SetEnabledAsync),
/// never a general-purpose edit, so a write-role user can't sneak an approval-adjacent field
/// through the create/edit form.
/// </summary>
public sealed class CreateJobDefinitionDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public JobType JobType { get; init; }
    public required string CronExpression { get; init; }
    public required string RunAsUser { get; init; }
    public int MemoryLimitMb { get; init; }
    public int MaxDurationMinutes { get; init; }
    public string? NotifyEmails { get; init; }
}
