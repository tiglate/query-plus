using Microsoft.EntityFrameworkCore;
using QueryPlus.Data.Context;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Data.Repositories;

public sealed class JobRunRepository(ApplicationDbContext db) : IJobRunRepository
{
    private static readonly JobRunStatus[] ActiveStatuses =
        [JobRunStatus.Queued, JobRunStatus.Starting, JobRunStatus.Running];

    public Task<JobRun?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => db.JobRuns.FirstOrDefaultAsync(r => r.IdJobRun == id, cancellationToken);

    public async Task<(IReadOnlyList<JobRun> Items, int TotalCount)> SearchAsync(
        int? jobDefinitionId,
        JobRunStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = db.JobRuns.AsNoTracking().AsQueryable();

        if (jobDefinitionId is not null)
        {
            query = query.Where(r => r.IdJobDefinition == jobDefinitionId.Value);
        }

        if (status is not null)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.StartedAt ?? r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<JobRun>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await db.JobRuns
            .AsNoTracking()
            .Where(r => ActiveStatuses.Contains(r.Status))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(JobRun run, CancellationToken cancellationToken = default)
        => await db.JobRuns.AddAsync(run, cancellationToken);

    public void Update(JobRun run) => db.JobRuns.Update(run);
}
