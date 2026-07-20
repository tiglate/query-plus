using QueryPlus.Domain.Enums;

namespace QueryPlus.Domain.Entities;

/// <summary>
/// tb_procedure_aud
/// </summary>
public class ProcedureAud
{
    public int IdProcedure { get; set; }
    public int IdRevision { get; set; }
    public RevisionTypeCode? IdRevisionType { get; set; }
    public int? IdCategory { get; set; }
    public string? Caption { get; set; }
    public string? DatabaseName { get; set; }
    public string? ProcedureName { get; set; }
    public bool? Enabled { get; set; }
    public bool? SupportsPagination { get; set; }
    public string? RoleEntitlement { get; set; }
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Revision Revision { get; set; } = null!;
    public RevisionType? RevisionType { get; set; }
}
