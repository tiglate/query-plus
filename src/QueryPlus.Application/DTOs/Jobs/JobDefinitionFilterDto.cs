using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.DTOs.Jobs;

public sealed class JobDefinitionFilterDto
{
    public string? Name { get; init; }
    public JobApprovalStatus? ApprovalStatus { get; init; }
    public bool? Enabled { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
