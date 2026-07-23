using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QueryPlus.Data.Context;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Data.Repositories;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    /// <remarks>
    /// Wrapped in an explicit transaction (via the execution strategy, per EF Core guidance)
    /// so that AuditSaveChangesInterceptor's post-save id correction for *_aud insert rows
    /// commits atomically with the principal insert instead of as a separate, unprotected
    /// follow-up statement.
    /// </remarks>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var result = await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    public ValueTask DisposeAsync() => context.DisposeAsync();
}
