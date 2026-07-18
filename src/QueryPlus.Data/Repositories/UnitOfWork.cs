using QueryPlus.Data.Context;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly QueryPlusDbContext _context;

    public UnitOfWork(QueryPlusDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}
