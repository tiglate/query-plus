using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using QueryPlus.Domain.Common;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Data.Interceptors;

/// <summary>
/// Skeleton interceptor that writes <c>tb_revision</c> + matching <c>*_aud</c> rows
/// for entities implementing <see cref="IAuditedEntity"/>.
/// </summary>
/// <remarks>
/// For INSERT, temporary identity keys are shared between the principal row and its audit row
/// so EF Core generates the same INT value for both after the round-trip.
/// </remarks>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IAuditContext _auditContext;

    public AuditSaveChangesInterceptor(IAuditContext auditContext)
    {
        _auditContext = auditContext;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var utcNow = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IHasTimestamps>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = utcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }

        var auditedEntries = context.ChangeTracker
            .Entries<IAuditedEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (auditedEntries.Count == 0)
        {
            return;
        }

        var revision = new Revision
        {
            Username = string.IsNullOrWhiteSpace(_auditContext.Username)
                ? "system"
                : _auditContext.Username,
            IpAddress = _auditContext.IpAddress,
            RevisionTimestamp = utcNow
        };

        context.Set<Revision>().Add(revision);

        foreach (var entry in auditedEntries)
        {
            var revisionType = entry.State switch
            {
                EntityState.Added => RevisionTypeCode.Insert,
                EntityState.Modified => RevisionTypeCode.Update,
                EntityState.Deleted => RevisionTypeCode.Delete,
                _ => throw new InvalidOperationException($"Unexpected state {entry.State}")
            };

            switch (entry.Entity)
            {
                case Category category:
                    AddCategoryAud(context, entry, category, revision, revisionType);
                    break;
                case Procedure procedure:
                    AddProcedureAud(context, entry, procedure, revision, revisionType);
                    break;
                case ProcedureParameter parameter:
                    AddParameterAud(context, entry, parameter, revision, revisionType);
                    break;
                case ProcedureColumn column:
                    AddColumnAud(context, entry, column, revision, revisionType);
                    break;
            }
        }
    }

    private static void AddCategoryAud(
        DbContext context,
        EntityEntry entry,
        Category entity,
        Revision revision,
        RevisionTypeCode revisionType)
    {
        var aud = new CategoryAud
        {
            IdCategory = entity.IdCategory,
            Revision = revision,
            IdRevisionType = revisionType,
            Description = Read<string?>(entry, nameof(Category.Description)),
            CreatedAt = Read<DateTime?>(entry, nameof(Category.CreatedAt)),
            UpdatedAt = Read<DateTime?>(entry, nameof(Category.UpdatedAt))
        };

        context.Set<CategoryAud>().Add(aud);
        ShareTemporaryKey(entry, nameof(Category.IdCategory), context.Entry(aud).Property(a => a.IdCategory));
    }

    private static void AddProcedureAud(
        DbContext context,
        EntityEntry entry,
        Procedure entity,
        Revision revision,
        RevisionTypeCode revisionType)
    {
        var aud = new ProcedureAud
        {
            IdProcedure = entity.IdProcedure,
            Revision = revision,
            IdRevisionType = revisionType,
            IdCategory = Read<int?>(entry, nameof(Procedure.IdCategory)),
            Caption = Read<string?>(entry, nameof(Procedure.Caption)),
            DatabaseName = Read<string?>(entry, nameof(Procedure.DatabaseName)),
            ProcedureName = Read<string?>(entry, nameof(Procedure.ProcedureName)),
            Enabled = Read<bool?>(entry, nameof(Procedure.Enabled)),
            RoleEntitlement = Read<string?>(entry, nameof(Procedure.RoleEntitlement)),
            Description = Read<string?>(entry, nameof(Procedure.Description)),
            CreatedAt = Read<DateTime?>(entry, nameof(Procedure.CreatedAt)),
            UpdatedAt = Read<DateTime?>(entry, nameof(Procedure.UpdatedAt))
        };

        context.Set<ProcedureAud>().Add(aud);
        ShareTemporaryKey(entry, nameof(Procedure.IdProcedure), context.Entry(aud).Property(a => a.IdProcedure));
    }

    private static void AddParameterAud(
        DbContext context,
        EntityEntry entry,
        ProcedureParameter entity,
        Revision revision,
        RevisionTypeCode revisionType)
    {
        var parameterType = entry.State == EntityState.Deleted
            ? entry.Property(nameof(ProcedureParameter.ParameterType)).OriginalValue?.ToString()
            : entity.ParameterType.ToString();

        var aud = new ProcedureParameterAud
        {
            IdProcedureParameter = entity.IdProcedureParameter,
            Revision = revision,
            IdRevisionType = revisionType,
            IdProcedure = Read<int?>(entry, nameof(ProcedureParameter.IdProcedure)),
            Caption = Read<string?>(entry, nameof(ProcedureParameter.Caption)),
            Name = Read<string?>(entry, nameof(ProcedureParameter.Name)),
            ParameterType = parameterType,
            DefaultValue = Read<string?>(entry, nameof(ProcedureParameter.DefaultValue)),
            ComboValues = Read<string?>(entry, nameof(ProcedureParameter.ComboValues)),
            IsRequired = Read<bool?>(entry, nameof(ProcedureParameter.IsRequired)),
            CreatedAt = Read<DateTime?>(entry, nameof(ProcedureParameter.CreatedAt)),
            UpdatedAt = Read<DateTime?>(entry, nameof(ProcedureParameter.UpdatedAt))
        };

        context.Set<ProcedureParameterAud>().Add(aud);
        ShareTemporaryKey(
            entry,
            nameof(ProcedureParameter.IdProcedureParameter),
            context.Entry(aud).Property(a => a.IdProcedureParameter));
    }

    private static void AddColumnAud(
        DbContext context,
        EntityEntry entry,
        ProcedureColumn entity,
        Revision revision,
        RevisionTypeCode revisionType)
    {
        var alignment = entry.State == EntityState.Deleted
            ? entry.Property(nameof(ProcedureColumn.Alignment)).OriginalValue?.ToString()
            : entity.Alignment.ToString();

        var aud = new ProcedureColumnAud
        {
            IdProcedureColumn = entity.IdProcedureColumn,
            Revision = revision,
            IdRevisionType = revisionType,
            IdProcedure = Read<int?>(entry, nameof(ProcedureColumn.IdProcedure)),
            TechnicalName = Read<string?>(entry, nameof(ProcedureColumn.TechnicalName)),
            Caption = Read<string?>(entry, nameof(ProcedureColumn.Caption)),
            Alignment = alignment,
            FormatMask = Read<string?>(entry, nameof(ProcedureColumn.FormatMask)),
            Visible = Read<bool?>(entry, nameof(ProcedureColumn.Visible)),
            CreatedAt = Read<DateTime?>(entry, nameof(ProcedureColumn.CreatedAt)),
            UpdatedAt = Read<DateTime?>(entry, nameof(ProcedureColumn.UpdatedAt))
        };

        context.Set<ProcedureColumnAud>().Add(aud);
        ShareTemporaryKey(
            entry,
            nameof(ProcedureColumn.IdProcedureColumn),
            context.Entry(aud).Property(a => a.IdProcedureColumn));
    }

    private static T Read<T>(EntityEntry entry, string propertyName)
    {
        var property = entry.Property(propertyName);
        var value = entry.State == EntityState.Deleted
            ? property.OriginalValue
            : property.CurrentValue;

        if (value is null)
        {
            return default!;
        }

        if (value is T typed)
        {
            return typed;
        }

        return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
    }

    private static void ShareTemporaryKey(
        EntityEntry principalEntry,
        string principalKeyName,
        PropertyEntry auditKeyProperty)
    {
        var principalKey = principalEntry.Property(principalKeyName);
        auditKeyProperty.CurrentValue = principalKey.CurrentValue;
        if (principalKey.IsTemporary)
        {
            auditKeyProperty.IsTemporary = true;
        }
    }
}
