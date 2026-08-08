using QueryPlus.Application.Common;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Application.Mapping;

/// <summary>
/// Entity → DTO projections for <see cref="Procedure"/>. The detail projection
/// hides pagination-reserved parameters (see <see cref="ProcedurePagination"/>)
/// and orders parameter/column collections by caption for stable UI rendering.
/// </summary>
public static class ProcedureMapper
{
    public static ProcedureListItemDto ToListItemDto(Procedure entity) => new()
    {
        Id = entity.IdProcedure,
        CategoryId = entity.IdCategory,
        CategoryDescription = entity.Category is null ? null : entity.Category.Description,
        Caption = entity.Caption,
        DatabaseName = entity.DatabaseName,
        ProcedureName = entity.ProcedureName,
        Enabled = entity.Enabled,
        SupportsPagination = entity.SupportsPagination,
        RoleEntitlement = entity.RoleEntitlement,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };

    public static IReadOnlyList<ProcedureListItemDto> ToListItemDtos(IEnumerable<Procedure> entities) =>
        entities.Select(ToListItemDto).ToArray();

    public static ProcedureLookupDto ToLookupDto(Procedure entity) => new()
    {
        Id = entity.IdProcedure,
        CategoryId = entity.IdCategory,
        CategoryDescription = entity.Category is null ? null : entity.Category.Description,
        Caption = entity.Caption,
        Description = entity.Description,
        RoleEntitlement = entity.RoleEntitlement,
        SupportsPagination = entity.SupportsPagination,
    };

    public static IReadOnlyList<ProcedureLookupDto> ToLookupDtos(IEnumerable<Procedure> entities) =>
        entities.Select(ToLookupDto).ToArray();

    public static ProcedureDetailDto ToDetailDto(Procedure entity) => new()
    {
        Id = entity.IdProcedure,
        CategoryId = entity.IdCategory,
        CategoryDescription = entity.Category is null ? null : entity.Category.Description,
        Caption = entity.Caption,
        DatabaseName = entity.DatabaseName,
        ProcedureName = entity.ProcedureName,
        Enabled = entity.Enabled,
        SupportsPagination = entity.SupportsPagination,
        RoleEntitlement = entity.RoleEntitlement,
        Description = entity.Description,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        Parameters = entity.Parameters
            .Where(parameter => !ProcedurePagination.IsReservedParameterName(parameter.Name))
            .OrderBy(parameter => parameter.Caption)
            .Select(ProcedureParameterMapper.ToDto)
            .ToArray(),
        Columns = entity.Columns
            .OrderBy(column => column.Caption)
            .Select(ProcedureColumnMapper.ToDto)
            .ToArray(),
    };
}
