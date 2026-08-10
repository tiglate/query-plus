using FluentValidation;
using Microsoft.Extensions.Options;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Common;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Jobs;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Mapping;
using QueryPlus.Application.Options;
using QueryPlus.Application.Validation;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Application.Services;

public sealed class JobDefinitionService(
    IJobDefinitionRepository jobs,
    IJobRunRequestRepository jobRunRequests,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser,
    IOptions<JobsOptions> jobsOptions,
    IValidator<CreateJobDefinitionDto> createValidator,
    IValidator<UpdateJobDefinitionDto> updateValidator,
    IValidator<RejectJobDefinitionDto> rejectValidator)
    : IJobDefinitionService
{
    public async Task<PagedResult<JobDefinitionListItemDto>> SearchAsync(
        JobDefinitionFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = PagedResult<JobDefinitionListItemDto>.Normalize(filter.Page, filter.PageSize);

        var (items, totalCount) = await jobs.SearchAsync(
            filter.Name,
            filter.ApprovalStatus,
            filter.Enabled,
            page,
            pageSize,
            cancellationToken);

        if (totalCount > 0 && (page - 1) * pageSize >= totalCount)
        {
            (page, pageSize) = PagedResult<JobDefinitionListItemDto>.Normalize(page, pageSize, totalCount);
            (items, totalCount) = await jobs.SearchAsync(
                filter.Name,
                filter.ApprovalStatus,
                filter.Enabled,
                page,
                pageSize,
                cancellationToken);
        }

        return new PagedResult<JobDefinitionListItemDto>
        {
            Items = JobMapper.ToListItemDtos(items),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<JobDefinitionDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await jobs.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : JobMapper.ToDetailDto(entity);
    }

    public async Task<JobDefinitionDetailDto> CreateAsync(
        CreateJobDefinitionDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(createValidator, dto, cancellationToken);

        var name = dto.Name.Trim();
        if (await jobs.ExistsByNameAsync(name, cancellationToken: cancellationToken))
        {
            throw new Common.ValidationException(nameof(dto.Name), "A job with this name already exists.");
        }

        var entity = new JobDefinition
        {
            Name = name,
            Description = dto.Description,
            JobType = dto.JobType,
            CronExpression = dto.CronExpression,
            RunAsUser = dto.RunAsUser,
            MemoryLimitMb = dto.MemoryLimitMb,
            MaxDurationMinutes = dto.MaxDurationMinutes,
            NotifyEmails = dto.NotifyEmails,
            ApprovalStatus = JobApprovalStatus.Draft,
            Enabled = false,
            CreatedBy = currentUser.Username
        };

        await jobs.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return JobMapper.ToDetailDto(entity);
    }

    public async Task<JobDefinitionDetailDto> UpdateAsync(
        int id,
        UpdateJobDefinitionDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(updateValidator, dto, cancellationToken);

        var entity = await jobs.GetByIdAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(nameof(JobDefinition), id);

        // Editing an Approved job in place would silently invalidate the guarantee that what was
        // approved (pinned by ScriptSha256) is what actually runs.
        if (entity.ApprovalStatus is not (JobApprovalStatus.Draft or JobApprovalStatus.Rejected))
        {
            throw new ForbiddenOperationException(
                "Only Draft or Rejected jobs can be edited. Submit a new approval cycle instead.");
        }

        var name = dto.Name.Trim();
        if (await jobs.ExistsByNameAsync(name, id, cancellationToken))
        {
            throw new Common.ValidationException(nameof(dto.Name), "A job with this name already exists.");
        }

        var previousJobType = entity.JobType;

        entity.Name = name;
        entity.Description = dto.Description;
        entity.JobType = dto.JobType;
        entity.CronExpression = dto.CronExpression;
        entity.RunAsUser = dto.RunAsUser;
        entity.MemoryLimitMb = dto.MemoryLimitMb;
        entity.MaxDurationMinutes = dto.MaxDurationMinutes;
        entity.NotifyEmails = dto.NotifyEmails;

        // A script uploaded for one JobType (a .sh file, say) is meaningless once the job is
        // redeclared as the other JobType - force a fresh upload rather than leave a stale,
        // wrong-extension script referenced by the job.
        if (dto.JobType != previousJobType && entity.ScriptPath is not null)
        {
            DeleteUploadedScriptBestEffort(id, previousJobType);
            entity.ScriptPath = null;
        }

        jobs.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return JobMapper.ToDetailDto(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await jobs.GetByIdAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(nameof(JobDefinition), id);

        if (entity.ApprovalStatus != JobApprovalStatus.Draft)
        {
            throw new ForbiddenOperationException("Only Draft jobs can be deleted.");
        }

        // Without this, a deleted job's uploaded script survives on disk under the trusted
        // ScriptAllowlistRoot tree with no remaining database record of it - the exact orphan
        // UpdateAsync's JobType-change path already guards against.
        if (entity.ScriptPath is not null)
        {
            DeleteUploadedScriptBestEffort(id, entity.JobType);
        }

        jobs.Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<JobDefinitionDetailDto> SubmitForApprovalAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var entity = await jobs.GetByIdAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(nameof(JobDefinition), id);

        if (entity.ApprovalStatus is not (JobApprovalStatus.Draft or JobApprovalStatus.Rejected))
        {
            throw new ForbiddenOperationException("Only Draft or Rejected jobs can be submitted for approval.");
        }

        if (entity.ScriptPath is null)
        {
            throw new Common.ValidationException(
                nameof(entity.ScriptPath), "No script has been uploaded for this job yet.");
        }

        EnsureScriptContained(entity);

        entity.ApprovalStatus = JobApprovalStatus.PendingApproval;
        jobs.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return JobMapper.ToDetailDto(entity);
    }

    public async Task<JobDefinitionDetailDto> ApproveAsync(
        int id,
        ApproveJobDefinitionDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await jobs.GetByIdAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(nameof(JobDefinition), id);

        if (entity.ApprovalStatus != JobApprovalStatus.PendingApproval)
        {
            throw new ForbiddenOperationException("Only jobs pending approval can be approved.");
        }

        // The module's core security invariant: the approver cannot be the requester. The DB
        // check constraint (ck_job_definition_no_self_approval) is defense-in-depth on top of
        // this - this is the actual, tested enforcement point.
        if (string.Equals(currentUser.Username, entity.CreatedBy, StringComparison.Ordinal))
        {
            throw new ForbiddenOperationException("A job cannot be approved by the same user who created it.");
        }

        if (entity.ScriptPath is null)
        {
            throw new Common.ValidationException(
                nameof(entity.ScriptPath), "No script has been uploaded for this job yet.");
        }

        var resolvedPath = EnsureScriptContained(entity);
        entity.ScriptSha256 = await JobScriptSecurity.ComputeSha256Async(resolvedPath, cancellationToken);

        entity.ApprovalStatus = JobApprovalStatus.Approved;
        entity.ApprovedBy = currentUser.Username;
        entity.ApprovedAt = DateTime.UtcNow;
        // Enabled is a separate, deliberate toggle - approval alone does not put the job on the
        // schedule (see SetEnabledAsync).

        jobs.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return JobMapper.ToDetailDto(entity);
    }

    public async Task<JobDefinitionDetailDto> RejectAsync(
        int id,
        RejectJobDefinitionDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(rejectValidator, dto, cancellationToken);

        var entity = await jobs.GetByIdAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(nameof(JobDefinition), id);

        if (entity.ApprovalStatus != JobApprovalStatus.PendingApproval)
        {
            throw new ForbiddenOperationException("Only jobs pending approval can be rejected.");
        }

        entity.ApprovalStatus = JobApprovalStatus.Rejected;
        entity.RejectionReason = dto.Reason.Trim();

        jobs.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return JobMapper.ToDetailDto(entity);
    }

    public async Task<JobDefinitionDetailDto> SetEnabledAsync(
        int id,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        var entity = await jobs.GetByIdAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(nameof(JobDefinition), id);

        if (entity.ApprovalStatus != JobApprovalStatus.Approved)
        {
            throw new ForbiddenOperationException("Only Approved jobs can be enabled.");
        }

        entity.Enabled = enabled;
        jobs.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return JobMapper.ToDetailDto(entity);
    }

    public async Task<JobRunRequestDto> RequestRunNowAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await jobs.GetByIdAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(nameof(JobDefinition), id);

        if (entity.ApprovalStatus != JobApprovalStatus.Approved || !entity.Enabled)
        {
            throw new ForbiddenOperationException("Only Approved and enabled jobs can be run now.");
        }

        var request = new JobRunRequest
        {
            IdJobDefinition = entity.IdJobDefinition,
            RequestedBy = currentUser.Username,
            RequestedAt = DateTime.UtcNow
        };

        await jobRunRequests.AddAsync(request, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return JobMapper.ToDto(request);
    }

    public async Task<JobDefinitionDetailDto> UploadScriptAsync(
        int id,
        Stream content,
        string originalFileName,
        long contentLength,
        CancellationToken cancellationToken = default)
    {
        var entity = await jobs.GetByIdAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(nameof(JobDefinition), id);

        // Mirrors UpdateAsync's rule exactly - editing an approved job's script is not allowed
        // without a fresh approval cycle.
        if (entity.ApprovalStatus is not (JobApprovalStatus.Draft or JobApprovalStatus.Rejected))
        {
            throw new ForbiddenOperationException(
                "Only Draft or Rejected jobs can have their script uploaded. Submit a new approval cycle instead.");
        }

        var expectedExtension = GetScriptExtension(entity.JobType);
        var actualExtension = Path.GetExtension(originalFileName);
        if (!string.Equals(actualExtension, expectedExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new Common.ValidationException(
                nameof(originalFileName),
                $"Expected a '{expectedExtension}' script for job type {entity.JobType}, but received " +
                $"'{(string.IsNullOrEmpty(actualExtension) ? "(no extension)" : actualExtension)}'.");
        }

        if (contentLength > jobsOptions.Value.MaxScriptUploadBytes)
        {
            throw new Common.ValidationException(
                nameof(contentLength),
                $"Script file exceeds the maximum allowed size of {jobsOptions.Value.MaxScriptUploadBytes} bytes.");
        }

        // Entirely server-constructed from a validated int id and a fixed literal - never from
        // client input - so there is no path-traversal surface here by construction.
        var uploadDirectory = GetUploadDirectory(id);
        Directory.CreateDirectory(uploadDirectory);

        // Keep the directory holding only the one current script - remove a leftover from a
        // previous JobType's extension, if any.
        foreach (var otherExtension in new[] { ".sh", ".py" })
        {
            if (otherExtension == expectedExtension)
            {
                continue;
            }

            var stalePath = Path.Combine(uploadDirectory, "run" + otherExtension);
            if (File.Exists(stalePath))
            {
                File.Delete(stalePath);
            }
        }

        var targetPath = Path.Combine(uploadDirectory, "run" + expectedExtension);
        await using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        entity.ScriptPath = targetPath;
        jobs.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return JobMapper.ToDetailDto(entity);
    }

    private string EnsureScriptContained(JobDefinition entity)
    {
        if (!JobScriptSecurity.TryResolveContainedPath(
                jobsOptions.Value.ScriptAllowlistRoot, entity.ScriptPath!, out var resolvedPath, out var error))
        {
            throw new Common.ValidationException(nameof(entity.ScriptPath), error!);
        }

        return resolvedPath;
    }

    private string GetUploadDirectory(int id) =>
        Path.Combine(jobsOptions.Value.ScriptAllowlistRoot, "uploads", "job-" + id);

    private static string GetScriptExtension(JobType jobType) => jobType switch
    {
        JobType.Shell => ".sh",
        JobType.Python => ".py",
        _ => throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "Unknown job type."),
    };

    private void DeleteUploadedScriptBestEffort(int id, JobType previousJobType)
    {
        try
        {
            var uploadDirectory = GetUploadDirectory(id);
            var previousPath = Path.Combine(uploadDirectory, "run" + GetScriptExtension(previousJobType));
            if (File.Exists(previousPath))
            {
                File.Delete(previousPath);
            }

            if (Directory.Exists(uploadDirectory))
            {
                Directory.Delete(uploadDirectory);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best-effort: a stray file-lock must not fail the whole update.
        }
    }
}
