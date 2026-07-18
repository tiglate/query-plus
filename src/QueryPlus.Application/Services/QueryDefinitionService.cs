using QueryPlus.Application.Interfaces;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Application.Services;

public class QueryDefinitionService : IQueryDefinitionService
{
    private readonly IRepository<QueryDefinition> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public QueryDefinitionService(
        IRepository<QueryDefinition> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public Task<QueryDefinition?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<QueryDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<QueryDefinition>> GetActiveAsync(CancellationToken cancellationToken = default)
        => _repository.FindAsync(q => q.IsActive, cancellationToken);

    public async Task<QueryDefinition> CreateAsync(
        QueryDefinition definition,
        CancellationToken cancellationToken = default)
    {
        definition.CreatedAt = DateTime.UtcNow;
        var created = await _repository.AddAsync(definition, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task UpdateAsync(QueryDefinition definition, CancellationToken cancellationToken = default)
    {
        definition.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(definition, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"QueryDefinition with Id {id} was not found.");

        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
