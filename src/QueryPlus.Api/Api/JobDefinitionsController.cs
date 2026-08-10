using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryPlus.Api.Security;
using QueryPlus.Application.Common;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Jobs;
using QueryPlus.Application.Interfaces;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Api.Api;

[ApiController]
[Route("api/jobs")]
public sealed class JobDefinitionsController(IJobDefinitionService jobs, IJobRunAsUserCatalog runAsUserCatalog)
    : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = AppRoles.CanReadJobs)]
    public Task<PagedResult<JobDefinitionListItemDto>> Search(string? name, JobApprovalStatus? approvalStatus,
        bool? enabled, int pageNumber = 1, int pageSize = PagedResult<JobDefinitionListItemDto>.DefaultPageSize,
        CancellationToken cancellationToken = default) => jobs.SearchAsync(
        new() { Name = name, ApprovalStatus = approvalStatus, Enabled = enabled, Page = pageNumber, PageSize = pageSize },
        cancellationToken);

    [HttpGet("{id:int}")]
    [Authorize(Roles = AppRoles.CanReadJobs)]
    public async Task<ActionResult<JobDefinitionDetailDto>> Get(int id, CancellationToken cancellationToken)
    {
        var result = await jobs.GetByIdAsync(id, cancellationToken);
        return result is null ? Problem(title: "Job definition not found", statusCode: 404) : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.CanWriteJobs)]
    public async Task<ActionResult<JobDefinitionDetailDto>> Create(CreateJobDefinitionDto request,
        CancellationToken cancellationToken)
    {
        var result = await jobs.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.CanWriteJobs)]
    public Task<JobDefinitionDetailDto> Update(int id, UpdateJobDefinitionDto request,
        CancellationToken cancellationToken) => jobs.UpdateAsync(id, CopyWithId(request, id), cancellationToken);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.CanWriteJobs)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await jobs.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/submit")]
    [Authorize(Roles = AppRoles.CanWriteJobs)]
    public Task<JobDefinitionDetailDto> Submit(int id, CancellationToken cancellationToken) =>
        jobs.SubmitForApprovalAsync(id, cancellationToken);

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = AppRoles.CanApproveJobs)]
    public Task<JobDefinitionDetailDto> Approve(int id, ApproveJobDefinitionDto request,
        CancellationToken cancellationToken) => jobs.ApproveAsync(id, request, cancellationToken);

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = AppRoles.CanApproveJobs)]
    public Task<JobDefinitionDetailDto> Reject(int id, RejectJobDefinitionDto request,
        CancellationToken cancellationToken) => jobs.RejectAsync(id, request, cancellationToken);

    [HttpPost("{id:int}/enabled")]
    [Authorize(Roles = AppRoles.CanWriteJobs)]
    public Task<JobDefinitionDetailDto> SetEnabled(int id, [FromBody] bool enabled,
        CancellationToken cancellationToken) => jobs.SetEnabledAsync(id, enabled, cancellationToken);

    [HttpPost("{id:int}/run-now")]
    [Authorize(Roles = AppRoles.CanWriteJobs)]
    public Task<JobRunRequestDto> RunNow(int id, CancellationToken cancellationToken) =>
        jobs.RequestRunNowAsync(id, cancellationToken);

    [HttpPost("{id:int}/script")]
    [Authorize(Roles = AppRoles.CanWriteJobs)]
    [RequestSizeLimit(2_097_152)]
    public Task<JobDefinitionDetailDto> UploadScript(int id, [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new ValidationException(nameof(file), "A non-empty script file is required.");
        }

        return jobs.UploadScriptAsync(id, file.OpenReadStream(), file.FileName, file.Length, cancellationToken);
    }

    [HttpGet("run-as-users")]
    [Authorize(Roles = AppRoles.CanWriteJobs)]
    public Task<IReadOnlyList<string>> GetRunAsUsers(CancellationToken cancellationToken) =>
        runAsUserCatalog.GetEligibleUsersAsync(cancellationToken);

    private static UpdateJobDefinitionDto CopyWithId(UpdateJobDefinitionDto x, int id) => new()
    {
        Id = id, Name = x.Name, Description = x.Description, JobType = x.JobType,
        CronExpression = x.CronExpression, RunAsUser = x.RunAsUser, MemoryLimitMb = x.MemoryLimitMb,
        MaxDurationMinutes = x.MaxDurationMinutes, NotifyEmails = x.NotifyEmails
    };
}
