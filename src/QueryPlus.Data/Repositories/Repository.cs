using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using QueryPlus.Data.Context;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Data.Repositories;

public class Repository<TEntity>(ApplicationDbContext context) : IRepository<TEntity>
    where TEntity : class
{
    protected readonly ApplicationDbContext Context = context;
    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await DbSet.FindAsync([id], cancellationToken);

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        => await DbSet.FindAsync([id], cancellationToken) is not null;

    public virtual Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        => predicate is null
            ? DbSet.CountAsync(cancellationToken)
            : DbSet.CountAsync(predicate, cancellationToken);
}
