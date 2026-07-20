using Microsoft.EntityFrameworkCore;
using QueryPlus.Data.Context;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Data.Repositories;

public sealed class CategoryRepository(ApplicationDbContext db) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => db.Categories.FirstOrDefaultAsync(c => c.IdCategory == id, cancellationToken);

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        => await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Description)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Category> Items, int TotalCount)> SearchAsync(
        string? description,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = db.Categories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(description))
        {
            var term = description.Trim();
            query = query.Where(c => c.Description.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Description)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> ExistsByDescriptionAsync(
        string description,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Categories.AsNoTracking()
            .Where(c => c.Description == description);

        if (excludeId is not null)
        {
            query = query.Where(c => c.IdCategory != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasProceduresAsync(int categoryId, CancellationToken cancellationToken = default)
        => db.Procedures.AsNoTracking().AnyAsync(p => p.IdCategory == categoryId, cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
        => await db.Categories.AddAsync(category, cancellationToken);

    public void Update(Category category) => db.Categories.Update(category);

    public void Remove(Category category) => db.Categories.Remove(category);
}
