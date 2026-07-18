using QueryPlus.Domain.Entities;

namespace QueryPlus.Application.Interfaces;

/// <summary>
/// Application service for managing query definitions (CRUD via EF Core).
/// </summary>
public interface IQueryDefinitionService
{
    Task<QueryDefinition?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QueryDefinition>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QueryDefinition>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<QueryDefinition> CreateAsync(QueryDefinition definition, CancellationToken cancellationToken = default);
    Task UpdateAsync(QueryDefinition definition, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
