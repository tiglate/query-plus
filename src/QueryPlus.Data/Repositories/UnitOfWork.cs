using QueryPlus.Data.Context;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Data.Repositories;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync() => context.DisposeAsync();
}
