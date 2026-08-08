using Microsoft.EntityFrameworkCore;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.Interfaces;
using QueryPlus.Data.Context;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Data.Repositories;

public sealed class ConfigurationAuditReader(ApplicationDbContext db) : IConfigurationAuditReader
{
    // Only the first Insert and last Update revision are ever needed, but the audit table for a
    // frequently-edited entity grows unboundedly (one row per edit, never purged). Two targeted
    // ORDER BY + TOP(1) queries push that selection to SQL instead of loading the full history.
    public async Task<AuditDetailsDto> GetCategoryAuditDetailsAsync(
        int categoryId,
        CancellationToken cancellationToken = default)
    {
        var createdBy = await db.CategoryAudits.AsNoTracking()
            .Where(a => a.IdCategory == categoryId && a.IdRevisionType == RevisionTypeCode.Insert)
            .OrderBy(a => a.Revision.RevisionTimestamp).ThenBy(a => a.IdRevision)
            .Select(a => a.Revision.Username)
            .FirstOrDefaultAsync(cancellationToken);

        var updatedBy = await db.CategoryAudits.AsNoTracking()
            .Where(a => a.IdCategory == categoryId && a.IdRevisionType == RevisionTypeCode.Update)
            .OrderByDescending(a => a.Revision.RevisionTimestamp).ThenByDescending(a => a.IdRevision)
            .Select(a => a.Revision.Username)
            .FirstOrDefaultAsync(cancellationToken);

        return new AuditDetailsDto { CreatedBy = createdBy, UpdatedBy = updatedBy };
    }

    public async Task<AuditDetailsDto> GetProcedureAuditDetailsAsync(
        int procedureId,
        CancellationToken cancellationToken = default)
    {
        var createdBy = await db.ProcedureAudits.AsNoTracking()
            .Where(a => a.IdProcedure == procedureId && a.IdRevisionType == RevisionTypeCode.Insert)
            .OrderBy(a => a.Revision.RevisionTimestamp).ThenBy(a => a.IdRevision)
            .Select(a => a.Revision.Username)
            .FirstOrDefaultAsync(cancellationToken);

        var updatedBy = await db.ProcedureAudits.AsNoTracking()
            .Where(a => a.IdProcedure == procedureId && a.IdRevisionType == RevisionTypeCode.Update)
            .OrderByDescending(a => a.Revision.RevisionTimestamp).ThenByDescending(a => a.IdRevision)
            .Select(a => a.Revision.Username)
            .FirstOrDefaultAsync(cancellationToken);

        return new AuditDetailsDto { CreatedBy = createdBy, UpdatedBy = updatedBy };
    }
}
