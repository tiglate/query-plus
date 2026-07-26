using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Application.Mapping;

/// <summary>
/// Entity → DTO projection for <see cref="ProcedureParameter"/>. The DTO exposes
/// <see cref="ProcedureParameterDto.ComboOptions"/> as a derived property parsed
/// lazily from <see cref="ProcedureParameterDto.ComboValues"/>; this mapper only
/// sets the raw field.
/// </summary>
public static class ProcedureParameterMapper
{
    public static ProcedureParameterDto ToDto(ProcedureParameter entity) => new()
    {
        Id = entity.IdProcedureParameter,
        Caption = entity.Caption,
        Name = entity.Name,
        ParameterType = entity.ParameterType,
        DefaultValue = entity.DefaultValue,
        ComboValues = entity.ComboValues,
        IsRequired = entity.IsRequired,
    };

    public static IReadOnlyList<ProcedureParameterDto> ToDtos(IEnumerable<ProcedureParameter> entities) =>
        entities.Select(ToDto).ToArray();
}
