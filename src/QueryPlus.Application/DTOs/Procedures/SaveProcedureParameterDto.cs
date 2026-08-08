using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.DTOs.Procedures;

public sealed class SaveProcedureParameterDto
{
    public int? Id { get; init; }
    public required string Caption { get; init; }
    public required string Name { get; init; }
    public ParameterType ParameterType { get; init; }
    public string? DefaultValue { get; init; }
    public string? ComboValues { get; init; }
    public bool IsRequired { get; init; }
    public bool IsSensitive { get; init; }
}
