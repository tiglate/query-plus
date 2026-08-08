using QueryPlus.Domain.Common;

namespace QueryPlus.Domain.Entities;

/// <summary>
/// tb_procedure
/// </summary>
public class Procedure : IHasTimestamps, IAuditedEntity
{
    public int IdProcedure { get; set; }
    public int IdCategory { get; set; }
    public required string Caption { get; set; }
    public required string DatabaseName { get; set; }
    public required string ProcedureName { get; set; }
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// When true, app injects @PageNumber/@PageSize and reads @TotalRecords OUTPUT.
    /// </summary>
    public bool SupportsPagination { get; set; }

    public required string RoleEntitlement { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Category Category { get; set; } = null!;
    public ICollection<ProcedureParameter> Parameters { get; set; } = new List<ProcedureParameter>();
    public ICollection<ProcedureColumn> Columns { get; set; } = new List<ProcedureColumn>();
    public ICollection<ExecutionLog> ExecutionLogs { get; set; } = new List<ExecutionLog>();

    /// <summary>
    /// Calculates parameters currently attached to this procedure that are omitted from the updated parameter IDs.
    /// </summary>
    public IReadOnlyList<ProcedureParameter> GetRemovedParameters(IEnumerable<int> updatedParameterIds)
    {
        var targetSet = updatedParameterIds.ToHashSet();
        return Parameters.Where(p => !targetSet.Contains(p.IdProcedureParameter)).ToList();
    }

    /// <summary>
    /// Calculates columns currently attached to this procedure that are omitted from the updated column IDs.
    /// </summary>
    public IReadOnlyList<ProcedureColumn> GetRemovedColumns(IEnumerable<int> updatedColumnIds)
    {
        var targetSet = updatedColumnIds.ToHashSet();
        return Columns.Where(c => !targetSet.Contains(c.IdProcedureColumn)).ToList();
    }

    /// <summary>
    /// Realm role that implies every permission system-wide, including running any catalogued
    /// procedure regardless of its own entitlement. Mirrors QueryPlus.Api.Security.AppRoles.Admin
    /// (kept as a literal here rather than a cross-layer reference, since Domain must not depend
    /// on Api) - keep the two in sync.
    /// </summary>
    private const string AdminRole = "ROLE_ADMIN";

    /// <summary>
    /// Whether a user holding <paramref name="userRoles"/> may see/execute this procedure.
    /// RoleEntitlement is a single role or a comma-separated list; an empty entitlement means
    /// the procedure is public (accessible to any authenticated user). This is the single
    /// source of truth for entitlement matching - callers must not reimplement the split/match
    /// rule themselves.
    /// </summary>
    public bool IsAccessibleTo(IReadOnlyCollection<string> userRoles)
    {
        if (userRoles.Contains(AdminRole, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        var required = RoleEntitlement
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (required.Length == 0)
        {
            return true;
        }

        return userRoles.Count > 0 &&
               required.Any(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }
}
