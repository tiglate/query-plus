using QueryPlus.Domain.Entities;

namespace QueryPlus.Domain.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> SearchAsync(
        string? description,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsByDescriptionAsync(
        string description,
        int? excludeId = null,
        CancellationToken cancellationToken = default);
    Task<bool> HasProceduresAsync(int categoryId, CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    void Update(Category category);
    void Remove(Category category);
}
