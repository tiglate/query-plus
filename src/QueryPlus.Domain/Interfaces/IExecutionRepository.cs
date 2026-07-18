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
}
