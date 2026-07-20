using Microsoft.EntityFrameworkCore;
using QueryPlus.Data.Context;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Data.Repositories;

public sealed class ExecutionRepository(ApplicationDbContext db) : IExecutionRepository
{
    public async Task AddAsync(ExecutionLog log, CancellationToken cancellationToken = default)
        => await db.ExecutionLogs.AddAsync(log, cancellationToken);

    public Task<ExecutionLog?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => db.ExecutionLogs.AsNoTracking()
            .FirstOrDefaultAsync(l => l.IdExecutionLog == id, cancellationToken);

    public async Task<IReadOnlyList<ExecutionLog>> GetByProcedureAsync(
        int procedureId,
        int take = 50,
        CancellationToken cancellationToken = default)
        => await db.ExecutionLogs.AsNoTracking()
            .Where(l => l.IdProcedure == procedureId)
            .OrderByDescending(l => l.ExecutionStart)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ExecutionLog>> GetByUsernameAsync(
        string username,
        int take = 50,
        CancellationToken cancellationToken = default)
        => await db.ExecutionLogs.AsNoTracking()
            .Where(l => l.Username == username)
            .OrderByDescending(l => l.ExecutionStart)
            .Take(take)
            .ToListAsync(cancellationToken);
}
