using Microsoft.EntityFrameworkCore;
using QueryPlus.Data.Context;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Data.Repositories;

public sealed class JobDefinitionRepository(ApplicationDbContext db) : IJobDefinitionRepository
{
    public Task<JobDefinition?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => db.JobDefinitions.FirstOrDefaultAsync(j => j.IdJobDefinition == id, cancellationToken);

    public async Task<(IReadOnlyList<JobDefinition> Items, int TotalCount)> SearchAsync(
        string? name,
        JobApprovalStatus? approvalStatus,
        bool? enabled,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = db.JobDefinitions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            var term = name.Trim();
            query = query.Where(j => j.Name.Contains(term));
        }

        if (approvalStatus is not null)
        {
            query = query.Where(j => j.ApprovalStatus == approvalStatus.Value);
        }

        if (enabled is not null)
        {
            query = query.Where(j => j.Enabled == enabled.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(j => j.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<JobDefinition>> GetApprovedEnabledAsync(
        CancellationToken cancellationToken = default)
        => await db.JobDefinitions
            .AsNoTracking()
            .Where(j => j.ApprovalStatus == JobApprovalStatus.Approved && j.Enabled)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByNameAsync(
        string name,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.JobDefinitions.AsNoTracking()
            .Where(j => j.Name == name);

        if (excludeId is not null)
        {
            query = query.Where(j => j.IdJobDefinition != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(JobDefinition job, CancellationToken cancellationToken = default)
        => await db.JobDefinitions.AddAsync(job, cancellationToken);

    public void Update(JobDefinition job) => db.JobDefinitions.Update(job);

    public void Remove(JobDefinition job) => db.JobDefinitions.Remove(job);
}
