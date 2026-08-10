using Microsoft.Extensions.Options;
using QueryPlus.Application.Common;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Jobs;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Mapping;
using QueryPlus.Application.Options;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Application.Services;

public sealed class JobRunService(
    IJobRunRepository jobRuns,
    IJobRunRequestRepository jobRunRequests,
    IOptions<JobsOptions> jobsOptions)
    : IJobRunService
{
    public async Task<PagedResult<JobRunListItemDto>> SearchAsync(
        JobRunFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = PagedResult<JobRunListItemDto>.Normalize(filter.Page, filter.PageSize);

        var (items, totalCount) = await jobRuns.SearchAsync(
            filter.JobDefinitionId,
            filter.Status,
            page,
            pageSize,
            cancellationToken);

        if (totalCount > 0 && (page - 1) * pageSize >= totalCount)
        {
            (page, pageSize) = PagedResult<JobRunListItemDto>.Normalize(page, pageSize, totalCount);
            (items, totalCount) = await jobRuns.SearchAsync(
                filter.JobDefinitionId,
                filter.Status,
                page,
                pageSize,
                cancellationToken);
        }

        return new PagedResult<JobRunListItemDto>
        {
            Items = JobMapper.ToListItemDtos(items),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<JobRunDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await jobRuns.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : JobMapper.ToDetailDto(entity);
    }

    public async Task<JobRunRequestDto?> GetRunRequestAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        var entity = await jobRunRequests.GetByIdAsync(requestId, cancellationToken);
        return entity is null ? null : JobMapper.ToDto(entity);
    }

    public async Task<string> ReadLogAsync(
        int runId,
        JobLogStream stream,
        CancellationToken cancellationToken = default)
    {
        var run = await jobRuns.GetByIdAsync(runId, cancellationToken)
                  ?? throw new EntityNotFoundException(nameof(JobRun), runId);

        var storedPath = stream == JobLogStream.Stdout ? run.StdoutPath : run.StderrPath;
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            throw new EntityNotFoundException(nameof(JobRun), runId);
        }

        // Never trust a DB-sourced path blindly - re-validate containment under Jobs:LogRoot
        // before opening the file, same discipline as script-path validation.
        if (!JobScriptSecurity.TryResolveContainedPath(
                jobsOptions.Value.LogRoot, storedPath, out var resolvedPath, out _))
        {
            throw new EntityNotFoundException(nameof(JobRun), runId);
        }

        return await File.ReadAllTextAsync(resolvedPath, cancellationToken);
    }
}
