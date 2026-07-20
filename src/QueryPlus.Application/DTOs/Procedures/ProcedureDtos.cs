using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.DTOs.Procedures;

public sealed class ProcedureListItemDto
{
    public int Id { get; init; }
    public int CategoryId { get; init; }
    public string? CategoryDescription { get; init; }
    public required string Caption { get; init; }
    public required string DatabaseName { get; init; }
    public required string ProcedureName { get; init; }
    public bool Enabled { get; init; }
    public bool SupportsPagination { get; init; }
    public required string RoleEntitlement { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class ProcedureLookupDto
{
    public int Id { get; init; }
    public int CategoryId { get; init; }
    public string? CategoryDescription { get; init; }
    public required string Caption { get; init; }
    public string? Description { get; init; }
    public required string RoleEntitlement { get; init; }
    public bool SupportsPagination { get; init; }
}

public sealed class ProcedureDetailDto
{
    public int Id { get; init; }
    public int CategoryId { get; init; }
    public string? CategoryDescription { get; init; }
    public required string Caption { get; init; }
    public required string DatabaseName { get; init; }
    public required string ProcedureName { get; init; }
    public bool Enabled { get; init; }
    public bool SupportsPagination { get; init; }
    public required string RoleEntitlement { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<ProcedureParameterDto> Parameters { get; init; } = [];
    public IReadOnlyList<ProcedureColumnDto> Columns { get; init; } = [];
}

public sealed class ProcedureParameterDto
{
    public int Id { get; init; }
    public required string Caption { get; init; }
    /// <summary>SQL parameter name, e.g. @StartDate.</summary>
    public required string Name { get; init; }
    public ParameterType ParameterType { get; init; }
    public string? DefaultValue { get; init; }
    /// <summary>JSON array string for Combo type.</summary>
    public string? ComboValues { get; init; }
    public bool IsRequired { get; init; }
    public IReadOnlyList<string> ComboOptions { get; init; } = [];
}

public sealed class ProcedureColumnDto
{
    public int Id { get; init; }
    public required string TechnicalName { get; init; }
    public required string Caption { get; init; }
    public ColumnAlignment Alignment { get; init; }
    public string? FormatMask { get; init; }
    public bool Visible { get; init; }
}

public sealed class ProcedureFilterDto
{
    public int? CategoryId { get; init; }
    public string? Caption { get; init; }
    public string? RoleEntitlement { get; init; }
    public bool? Enabled { get; init; }
    public string? DatabaseName { get; init; }
    public string? ProcedureName { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class SaveProcedureDto
{
    public int? Id { get; init; }
    public int CategoryId { get; init; }
    public required string Caption { get; init; }
    public required string DatabaseName { get; init; }
    public required string ProcedureName { get; init; }
    public bool Enabled { get; init; } = true;
    public bool SupportsPagination { get; init; }
    public required string RoleEntitlement { get; init; }
    public string? Description { get; init; }
    public IList<SaveProcedureParameterDto> Parameters { get; init; } = [];
    public IList<SaveProcedureColumnDto> Columns { get; init; } = [];
}

public sealed class SaveProcedureParameterDto
{
    public int? Id { get; init; }
    public required string Caption { get; init; }
    public required string Name { get; init; }
    public ParameterType ParameterType { get; init; }
    public string? DefaultValue { get; init; }
    public string? ComboValues { get; init; }
    public bool IsRequired { get; init; }
}

public sealed class SaveProcedureColumnDto
{
    public int? Id { get; init; }
    public required string TechnicalName { get; init; }
    public required string Caption { get; init; }
    public ColumnAlignment Alignment { get; init; } = ColumnAlignment.Left;
    public string? FormatMask { get; init; }
    public bool Visible { get; init; } = true;
}
