using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryPlus.Api.Security;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Jobs;
using QueryPlus.Application.Interfaces;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Api.Api;

[ApiController]
[Route("api/jobs/runs")]
public sealed class JobRunsController(IJobRunService jobRuns) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = AppRoles.CanReadJobs)]
    public Task<PagedResult<JobRunListItemDto>> Search(int? jobDefinitionId, JobRunStatus? status,
        int pageNumber = 1, int pageSize = PagedResult<JobRunListItemDto>.DefaultPageSize,
        CancellationToken cancellationToken = default) => jobRuns.SearchAsync(
        new() { JobDefinitionId = jobDefinitionId, Status = status, Page = pageNumber, PageSize = pageSize },
        cancellationToken);

    [HttpGet("{id:int}")]
    [Authorize(Roles = AppRoles.CanReadJobs)]
    public async Task<ActionResult<JobRunDetailDto>> Get(int id, CancellationToken cancellationToken)
    {
        var result = await jobRuns.GetByIdAsync(id, cancellationToken);
        return result is null ? Problem(title: "Job run not found", statusCode: 404) : Ok(result);
    }

    [HttpGet("requests/{requestId:int}")]
    [Authorize(Roles = AppRoles.CanReadJobs)]
    public async Task<ActionResult<JobRunRequestDto>> GetRequest(int requestId, CancellationToken cancellationToken)
    {
        var result = await jobRuns.GetRunRequestAsync(requestId, cancellationToken);
        return result is null ? Problem(title: "Job run request not found", statusCode: 404) : Ok(result);
    }

    [HttpGet("{id:int}/logs/{stream}")]
    [Authorize(Roles = AppRoles.CanReadJobs)]
    public async Task<IActionResult> Logs(int id, JobLogStream stream, CancellationToken cancellationToken)
    {
        var content = await jobRuns.ReadLogAsync(id, stream, cancellationToken);
        return Content(content, "text/plain");
    }
}
