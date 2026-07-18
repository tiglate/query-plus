using Microsoft.EntityFrameworkCore;
using QueryPlus.Data.Context;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Data.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _db;

    public CategoryRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Categories.FirstOrDefaultAsync(c => c.IdCategory == id, cancellationToken);

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Description)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Category>> SearchAsync(
        string? description,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Categories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(description))
        {
            var term = description.Trim();
            query = query.Where(c => c.Description.Contains(term));
        }

        return await query
            .OrderBy(c => c.Description)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByDescriptionAsync(
        string description,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Categories.AsNoTracking()
            .Where(c => c.Description == description);

        if (excludeId is not null)
        {
            query = query.Where(c => c.IdCategory != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasProceduresAsync(int categoryId, CancellationToken cancellationToken = default)
        => _db.Procedures.AsNoTracking().AnyAsync(p => p.IdCategory == categoryId, cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
        => await _db.Categories.AddAsync(category, cancellationToken);

    public void Update(Category category) => _db.Categories.Update(category);

    public void Remove(Category category) => _db.Categories.Remove(category);
}
