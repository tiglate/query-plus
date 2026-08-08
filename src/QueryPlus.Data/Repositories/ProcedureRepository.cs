using Microsoft.EntityFrameworkCore;
using QueryPlus.Data.Context;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Data.Repositories;

public sealed class ProcedureRepository(ApplicationDbContext db) : IProcedureRepository
{
    public Task<Procedure?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => db.Procedures.FirstOrDefaultAsync(p => p.IdProcedure == id, cancellationToken);

    public async Task<IReadOnlyList<Procedure>> GetAllAsync(CancellationToken cancellationToken = default)
        => await db.Procedures
            .AsNoTracking()
            .OrderBy(p => p.Caption)
            .ToListAsync(cancellationToken);

    // Tracked: ProcedureService.UpdateAsync/DeleteAsync mutate the entity returned by this method.
    public Task<Procedure?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => db.Procedures
            .AsSplitQuery()
            .Include(p => p.Category)
            .Include(p => p.Parameters)
            .Include(p => p.Columns)
            .FirstOrDefaultAsync(p => p.IdProcedure == id, cancellationToken);

    // Read-only: used only to build execution parameters/grid metadata (ExecutionService, the
    // export worker) - never saved through, so no change tracking is needed.
    public Task<Procedure?> GetEnabledByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => db.Procedures
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Category)
            .Include(p => p.Parameters)
            .Include(p => p.Columns)
            .FirstOrDefaultAsync(p => p.IdProcedure == id && p.Enabled, cancellationToken);

    public async Task<(IReadOnlyList<Procedure> Items, int TotalCount)> SearchAsync(
        ProcedureSearchCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = db.Procedures
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        if (criteria.CategoryId is not null)
        {
            query = query.Where(p => p.IdCategory == criteria.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Caption))
        {
            var term = criteria.Caption.Trim();
            query = query.Where(p => p.Caption.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(criteria.RoleEntitlement))
        {
            var role = criteria.RoleEntitlement.Trim();
            query = query.Where(p => p.RoleEntitlement.Contains(role));
        }

        if (criteria.Enabled is not null)
        {
            query = query.Where(p => p.Enabled == criteria.Enabled.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.DatabaseName))
        {
            var database = criteria.DatabaseName.Trim();
            query = query.Where(p => p.DatabaseName == database);
        }

        if (!string.IsNullOrWhiteSpace(criteria.ProcedureName))
        {
            var name = criteria.ProcedureName.Trim();
            query = query.Where(p => p.ProcedureName.Contains(name));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Caption)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Procedure>> GetAccessibleForExecutionAsync(
        IReadOnlyCollection<string> userRoles,
        CancellationToken cancellationToken = default)
    {
        var query = db.Procedures
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Enabled);

        var items = await query.ToListAsync(cancellationToken);

        // Home list groups by category (A–Z), procedures by caption within each group.
        return items
            .Where(p => p.IsAccessibleTo(userRoles))
            .OrderBy(p => p.Category?.Description ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(p => p.Caption, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public Task<bool> ExistsByCaptionAsync(
        string caption,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Procedures.AsNoTracking().Where(p => p.Caption == caption);
        if (excludeId is not null)
        {
            query = query.Where(p => p.IdProcedure != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> ExistsByDatabaseAndNameAsync(
        string connectionName,
        string databaseName,
        string procedureName,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Procedures.AsNoTracking()
            .Where(p => p.ConnectionName == connectionName && p.DatabaseName == databaseName &&
                        p.ProcedureName == procedureName);

        if (excludeId is not null)
        {
            query = query.Where(p => p.IdProcedure != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Procedure procedure, CancellationToken cancellationToken = default)
        => await db.Procedures.AddAsync(procedure, cancellationToken);

    public void Update(Procedure procedure) => db.Procedures.Update(procedure);

    public void Remove(Procedure procedure) => db.Procedures.Remove(procedure);

    public void RemoveParameter(ProcedureParameter parameter)
        => db.ProcedureParameters.Remove(parameter);

    public void RemoveColumn(ProcedureColumn column)
        => db.ProcedureColumns.Remove(column);

    public async Task EnsureCategoryLoadedAsync(Procedure procedure, CancellationToken cancellationToken = default)
    {
        var entry = db.Entry(procedure).Reference(p => p.Category);
        if (!entry.IsLoaded)
        {
            await entry.LoadAsync(cancellationToken);
        }
    }
}
