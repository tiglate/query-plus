using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Jobs;

namespace QueryPlus.Application.Interfaces;

public interface IJobDefinitionService
{
    Task<PagedResult<JobDefinitionListItemDto>> SearchAsync(
        JobDefinitionFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<JobDefinitionDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<JobDefinitionDetailDto> CreateAsync(
        CreateJobDefinitionDto dto,
        CancellationToken cancellationToken = default);

    Task<JobDefinitionDetailDto> UpdateAsync(
        int id,
        UpdateJobDefinitionDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>Draft-only - once submitted, use Reject to abandon a proposal.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<JobDefinitionDetailDto> SubmitForApprovalAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Pins ScriptSha256. Throws ForbiddenOperationException if the caller created the job.</summary>
    Task<JobDefinitionDetailDto> ApproveAsync(
        int id,
        ApproveJobDefinitionDto dto,
        CancellationToken cancellationToken = default);

    Task<JobDefinitionDetailDto> RejectAsync(
        int id,
        RejectJobDefinitionDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>Only permitted while ApprovalStatus is Approved.</summary>
    Task<JobDefinitionDetailDto> SetEnabledAsync(int id, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>Queues a manual run, drained by QueryPlus.SchedulerSync.</summary>
    Task<JobRunRequestDto> RequestRunNowAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the uploaded script for a Draft/Rejected job and sets ScriptPath. Only permitted
    /// while ApprovalStatus is Draft or Rejected.
    /// </summary>
    Task<JobDefinitionDetailDto> UploadScriptAsync(
        int id,
        Stream content,
        string originalFileName,
        long contentLength,
        CancellationToken cancellationToken = default);
}
