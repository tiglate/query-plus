namespace QueryPlus.Application.DTOs.Procedures;

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
