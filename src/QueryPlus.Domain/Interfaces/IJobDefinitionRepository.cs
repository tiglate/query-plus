using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Domain.Interfaces;

public interface IJobDefinitionRepository
{
    Task<JobDefinition?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Filtered job definition search (admin screen) with server-side pagination.
    /// </summary>
    Task<(IReadOnlyList<JobDefinition> Items, int TotalCount)> SearchAsync(
        string? name,
        JobApprovalStatus? approvalStatus,
        bool? enabled,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>What QueryPlus.SchedulerSync polls every reconcile tick.</summary>
    Task<IReadOnlyList<JobDefinition>> GetApprovedEnabledAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(JobDefinition job, CancellationToken cancellationToken = default);
    void Update(JobDefinition job);
    void Remove(JobDefinition job);
}
