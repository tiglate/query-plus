using QueryPlus.Domain.Entities;

namespace QueryPlus.Domain.Interfaces;

public interface IJobRunRequestRepository
{
    Task<JobRunRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Unconsumed "Run Now" requests - what QueryPlus.SchedulerSync drains each reconcile tick.</summary>
    Task<IReadOnlyList<JobRunRequest>> GetPendingAsync(CancellationToken cancellationToken = default);

    Task AddAsync(JobRunRequest request, CancellationToken cancellationToken = default);
    void Update(JobRunRequest request);
}
