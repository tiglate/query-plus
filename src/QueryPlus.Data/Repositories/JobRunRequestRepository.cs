using Microsoft.EntityFrameworkCore;
using QueryPlus.Data.Context;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Data.Repositories;

public sealed class JobRunRequestRepository(ApplicationDbContext db) : IJobRunRequestRepository
{
    public Task<JobRunRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => db.JobRunRequests.FirstOrDefaultAsync(r => r.IdJobRunRequest == id, cancellationToken);

    public async Task<IReadOnlyList<JobRunRequest>> GetPendingAsync(CancellationToken cancellationToken = default)
        => await db.JobRunRequests
            .AsNoTracking()
            .Where(r => r.ConsumedAt == null)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(JobRunRequest request, CancellationToken cancellationToken = default)
        => await db.JobRunRequests.AddAsync(request, cancellationToken);

    public void Update(JobRunRequest request) => db.JobRunRequests.Update(request);
}
