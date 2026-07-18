namespace QueryPlus.Domain.Interfaces;

/// <summary>
/// Unit of Work for coordinating EF Core changes across repositories.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
