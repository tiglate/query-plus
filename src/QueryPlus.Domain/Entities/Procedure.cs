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
}
