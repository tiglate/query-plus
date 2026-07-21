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

    public async Task<(IReadOnlyList<ExecutionLog> Items, int TotalCount)> SearchAsync(
        ExecutionLogSearchCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = db.ExecutionLogs
            .AsNoTracking()
            .Include(l => l.Procedure)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Username))
        {
            var term = criteria.Username.Trim();
            query = query.Where(l => l.Username.Contains(term));
        }

        if (criteria.ProcedureId is not null)
        {
            query = query.Where(l => l.IdProcedure == criteria.ProcedureId.Value);
        }

        if (criteria.Success is not null)
        {
            query = query.Where(l => l.Success == criteria.Success.Value);
        }

        if (criteria.StartFrom is not null)
        {
            query = query.Where(l => l.ExecutionStart >= criteria.StartFrom.Value);
        }

        if (criteria.StartTo is not null)
        {
            query = query.Where(l => l.ExecutionStart < criteria.StartTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.ExecutionStart)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
