using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.DTOs.Jobs;

public sealed class JobRunFilterDto
{
    public int? JobDefinitionId { get; init; }
    public JobRunStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
