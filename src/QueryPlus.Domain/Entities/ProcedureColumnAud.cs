using QueryPlus.Domain.Enums;

namespace QueryPlus.Domain.Entities;

/// <summary>
/// tb_procedure_column_aud
/// </summary>
public class ProcedureColumnAud
{
    public int IdProcedureColumn { get; set; }
    public int IdRevision { get; set; }
    public RevisionTypeCode? IdRevisionType { get; set; }
    public int? IdProcedure { get; set; }
    public string? TechnicalName { get; set; }
    public string? Caption { get; set; }
    public string? Alignment { get; set; }
    public string? FormatMask { get; set; }
    public bool? Visible { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Revision Revision { get; set; } = null!;
    public RevisionType? RevisionType { get; set; }
}
