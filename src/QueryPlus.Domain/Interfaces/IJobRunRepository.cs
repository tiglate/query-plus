using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Domain.Interfaces;

public interface IJobRunRepository
{
    Task<JobRun?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Filtered run-history search with server-side pagination.
    /// </summary>
    Task<(IReadOnlyList<JobRun> Items, int TotalCount)> SearchAsync(
        int? jobDefinitionId,
        JobRunStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Queued/Starting/Running rows - what the watchdog scans for stale heartbeats.</summary>
    Task<IReadOnlyList<JobRun>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task AddAsync(JobRun run, CancellationToken cancellationToken = default);
    void Update(JobRun run);
}
