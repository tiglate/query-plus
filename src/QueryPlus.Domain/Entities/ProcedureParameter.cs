using QueryPlus.Domain.Common;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Domain.Entities;

/// <summary>
/// tb_procedure_parameter
/// </summary>
public class ProcedureParameter : IHasTimestamps, IAuditedEntity
{
    public int IdProcedureParameter { get; set; }
    public int IdProcedure { get; set; }
    public required string Caption { get; set; }
    public required string Name { get; set; }
    public ParameterType ParameterType { get; set; }
    public string? DefaultValue { get; set; }
    /// <summary>JSON array of combo options.</summary>
    public string? ComboValues { get; set; }
    /// <summary>When true, execution requires a non-empty value (or a configured default).</summary>
    public bool IsRequired { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Procedure Procedure { get; set; } = null!;
}
