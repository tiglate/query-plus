using QueryPlus.Domain.Common;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Domain.Entities;

/// <summary>
/// tb_procedure_column
/// </summary>
public class ProcedureColumn : IHasTimestamps, IAuditedEntity
{
    public int IdProcedureColumn { get; set; }
    public int IdProcedure { get; set; }
    public required string TechnicalName { get; set; }
    public required string Caption { get; set; }
    public ColumnAlignment Alignment { get; set; } = ColumnAlignment.Left;
    public string? FormatMask { get; set; }
    public bool Visible { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Procedure Procedure { get; set; } = null!;
}
