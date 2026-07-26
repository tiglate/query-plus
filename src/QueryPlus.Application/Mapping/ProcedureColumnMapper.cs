using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Application.Mapping;

/// <summary>
/// Entity → DTO projections for <see cref="ProcedureColumn"/>. Two target
/// shapes: the full admin <see cref="ProcedureColumnDto"/> and the lighter
/// runtime <see cref="GridColumnDto"/> used by the results grid.
/// </summary>
public static class ProcedureColumnMapper
{
    public static ProcedureColumnDto ToDto(ProcedureColumn entity) => new()
    {
        Id = entity.IdProcedureColumn,
        TechnicalName = entity.TechnicalName,
        Caption = entity.Caption,
        Alignment = entity.Alignment,
        FormatMask = entity.FormatMask,
        Visible = entity.Visible,
    };

    public static IReadOnlyList<ProcedureColumnDto> ToDtos(IEnumerable<ProcedureColumn> entities) =>
        entities.Select(ToDto).ToArray();

    public static GridColumnDto ToGridColumnDto(ProcedureColumn entity) => new()
    {
        TechnicalName = entity.TechnicalName,
        Caption = entity.Caption,
        Alignment = entity.Alignment,
        FormatMask = entity.FormatMask,
        Visible = entity.Visible,
    };

    public static IReadOnlyList<GridColumnDto> ToGridColumnDtos(IEnumerable<ProcedureColumn> entities) =>
        entities.Select(ToGridColumnDto).ToArray();
}
