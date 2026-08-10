using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Jobs;

namespace QueryPlus.Application.Interfaces;

public enum JobLogStream
{
    Stdout,
    Stderr
}

public interface IJobRunService
{
    Task<PagedResult<JobRunListItemDto>> SearchAsync(
        JobRunFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<JobRunDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<JobRunRequestDto?> GetRunRequestAsync(int requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-validates that the DB-sourced log path is contained under Jobs:LogRoot before opening
    /// the file - a stored path is not trusted blindly, same discipline as script-path validation.
    /// </summary>
    Task<string> ReadLogAsync(int runId, JobLogStream stream, CancellationToken cancellationToken = default);
}
