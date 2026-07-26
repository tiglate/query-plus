namespace QueryPlus.Application.DTOs.Procedures;

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
