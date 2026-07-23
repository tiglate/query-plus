using Microsoft.EntityFrameworkCore;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.Interfaces;
using QueryPlus.Data.Context;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Data.Repositories;

public sealed class ConfigurationAuditReader(ApplicationDbContext db) : IConfigurationAuditReader
{
    public async Task<AuditDetailsDto> GetCategoryAuditDetailsAsync(
        int categoryId,
        CancellationToken cancellationToken = default)
    {
        var revisions = await db.CategoryAudits
            .AsNoTracking()
            .Where(a => a.IdCategory == categoryId)
            .Select(a => new AuditRevision(a.IdRevisionType, a.Revision.Username, a.Revision.RevisionTimestamp, a.IdRevision))
            .ToListAsync(cancellationToken);

        return BuildDetails(revisions);
    }

    public async Task<AuditDetailsDto> GetProcedureAuditDetailsAsync(
        int procedureId,
        CancellationToken cancellationToken = default)
    {
        var revisions = await db.ProcedureAudits
            .AsNoTracking()
            .Where(a => a.IdProcedure == procedureId)
            .Select(a => new AuditRevision(a.IdRevisionType, a.Revision.Username, a.Revision.RevisionTimestamp, a.IdRevision))
            .ToListAsync(cancellationToken);

        return BuildDetails(revisions);
    }

    private static AuditDetailsDto BuildDetails(IEnumerable<AuditRevision> revisions)
    {
        var ordered = revisions
            .OrderBy(r => r.Timestamp)
            .ThenBy(r => r.Id)
            .ToList();

        return new AuditDetailsDto
        {
            CreatedBy = ordered.FirstOrDefault(r => r.Type == RevisionTypeCode.Insert)?.Username,
            UpdatedBy = ordered.LastOrDefault(r => r.Type == RevisionTypeCode.Update)?.Username
        };
    }

    private sealed record AuditRevision(
        RevisionTypeCode? Type,
        string Username,
        DateTime Timestamp,
        int Id);
}
