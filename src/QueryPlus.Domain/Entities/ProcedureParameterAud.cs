using QueryPlus.Domain.Enums;

namespace QueryPlus.Domain.Entities;

/// <summary>
/// tb_procedure_parameter_aud
/// </summary>
public class ProcedureParameterAud
{
    public int IdProcedureParameter { get; set; }
    public int IdRevision { get; set; }
    public RevisionTypeCode? IdRevisionType { get; set; }
    public int? IdProcedure { get; set; }
    public string? Caption { get; set; }
    public string? Name { get; set; }
    public string? ParameterType { get; set; }
    public string? DefaultValue { get; set; }
    public string? ComboValues { get; set; }
    public bool? IsRequired { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Revision Revision { get; set; } = null!;
    public RevisionType? RevisionType { get; set; }
}
