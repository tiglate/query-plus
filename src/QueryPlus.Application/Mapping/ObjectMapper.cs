using QueryPlus.Application.Common;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Application.Mapping;

/// <summary>
/// Hand-written entity → DTO mappers that replace the previous AutoMapper profile.
/// All maps are explicit and side-effect-free. Caller is responsible for
/// populating fields the mapper intentionally leaves unset (audit CreatedBy/UpdatedBy).
/// </summary>
public static class ObjectMapper
{
    public static CategoryListItemDto Map(Category entity) => new()
    {
        Id = entity.IdCategory,
        Description = entity.Description,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };

    public static IReadOnlyList<CategoryListItemDto> Map(IEnumerable<Category> entities) =>
        entities.Select(Map).ToArray();

    public static CategoryDetailDto MapDetail(Category entity) => new()
    {
        Id = entity.IdCategory,
        Description = entity.Description,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };

    public static ProcedureListItemDto Map(Procedure entity) => new()
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

    public static IReadOnlyList<ProcedureListItemDto> Map(IEnumerable<Procedure> entities) =>
        entities.Select(Map).ToArray();

    public static ProcedureLookupDto MapLookup(Procedure entity) => new()
    {
        Id = entity.IdProcedure,
        CategoryId = entity.IdCategory,
        CategoryDescription = entity.Category is null ? null : entity.Category.Description,
        Caption = entity.Caption,
        Description = entity.Description,
        RoleEntitlement = entity.RoleEntitlement,
        SupportsPagination = entity.SupportsPagination,
    };

    public static IReadOnlyList<ProcedureLookupDto> MapLookup(IEnumerable<Procedure> entities) =>
        entities.Select(MapLookup).ToArray();

    public static ProcedureDetailDto MapDetail(Procedure entity) => new()
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
            .Select(MapParameter)
            .ToArray(),
        Columns = entity.Columns
            .OrderBy(column => column.Caption)
            .Select(MapColumn)
            .ToArray(),
    };

    public static ProcedureParameterDto MapParameter(ProcedureParameter entity) => new()
    {
        Id = entity.IdProcedureParameter,
        Caption = entity.Caption,
        Name = entity.Name,
        ParameterType = entity.ParameterType,
        DefaultValue = entity.DefaultValue,
        ComboValues = entity.ComboValues,
        IsRequired = entity.IsRequired,
        ComboOptions = JsonHelpers.ParseStringArray(entity.ComboValues),
    };

    public static IReadOnlyList<ProcedureParameterDto> MapParameter(IEnumerable<ProcedureParameter> entities) =>
        entities.Select(MapParameter).ToArray();

    public static ProcedureColumnDto MapColumn(ProcedureColumn entity) => new()
    {
        Id = entity.IdProcedureColumn,
        TechnicalName = entity.TechnicalName,
        Caption = entity.Caption,
        Alignment = entity.Alignment,
        FormatMask = entity.FormatMask,
        Visible = entity.Visible,
    };

    public static IReadOnlyList<ProcedureColumnDto> MapColumn(IEnumerable<ProcedureColumn> entities) =>
        entities.Select(MapColumn).ToArray();

    public static GridColumnDto MapGridColumn(ProcedureColumn entity) => new()
    {
        TechnicalName = entity.TechnicalName,
        Caption = entity.Caption,
        Alignment = entity.Alignment,
        FormatMask = entity.FormatMask,
        Visible = entity.Visible,
    };

    public static IReadOnlyList<GridColumnDto> MapGridColumn(IEnumerable<ProcedureColumn> entities) =>
        entities.Select(MapGridColumn).ToArray();

    public static ExecutionLogDto MapLog(ExecutionLog entity) => new()
    {
        Id = entity.IdExecutionLog,
        ProcedureId = entity.IdProcedure,
        Username = entity.Username,
        IpAddress = entity.IpAddress,
        ExecutionStart = entity.ExecutionStart,
        ExecutionEnd = entity.ExecutionEnd,
        Success = entity.Success,
        ErrorMessage = entity.ErrorMessage,
        ParameterValuesJson = entity.ParameterValues,
        RowCount = entity.RowCount,
    };

    public static IReadOnlyList<ExecutionLogDto> MapLog(IEnumerable<ExecutionLog> entities) =>
        entities.Select(MapLog).ToArray();

    public static ExecutionLogListItemDto MapLogListItem(ExecutionLog entity) => new()
    {
        Id = entity.IdExecutionLog,
        ProcedureId = entity.IdProcedure,
        ProcedureCaption = entity.Procedure is null ? string.Empty : entity.Procedure.Caption,
        Username = entity.Username,
        IpAddress = entity.IpAddress,
        ExecutionStart = entity.ExecutionStart,
        ExecutionEnd = entity.ExecutionEnd,
        Success = entity.Success,
        ErrorMessage = entity.ErrorMessage,
        RowCount = entity.RowCount,
    };

    public static IReadOnlyList<ExecutionLogListItemDto> MapLogListItem(IEnumerable<ExecutionLog> entities) =>
        entities.Select(MapLogListItem).ToArray();
}
