using QueryPlus.Domain.Entities;

namespace QueryPlus.Domain.Interfaces;

public interface IExecutionRepository
{
    Task AddAsync(ExecutionLog log, CancellationToken cancellationToken = default);
    Task<ExecutionLog?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExecutionLog>> GetByProcedureAsync(
        int procedureId,
        int take = 50,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExecutionLog>> GetByUsernameAsync(
        string username,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Filtered execution log search (admin screen) with server-side pagination.
    /// </summary>
    Task<(IReadOnlyList<ExecutionLog> Items, int TotalCount)> SearchAsync(
        ExecutionLogSearchCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public sealed class ExecutionLogSearchCriteria
{
    public string? Username { get; init; }
    public int? ProcedureId { get; init; }
    public bool? Success { get; init; }

    /// <summary>Inclusive lower bound (UTC) on ExecutionStart.</summary>
    public DateTime? StartFrom { get; init; }

    /// <summary>Exclusive upper bound (UTC) on ExecutionStart.</summary>
    public DateTime? StartTo { get; init; }
}
