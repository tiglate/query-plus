namespace QueryPlus.Application.DTOs.Procedures;

public sealed class ProcedureDetailDto
{
    public int Id { get; init; }
    public int CategoryId { get; init; }
    public string? CategoryDescription { get; init; }
    public required string Caption { get; init; }
    public required string ConnectionName { get; init; }
    public required string DatabaseName { get; init; }
    public required string ProcedureName { get; init; }
    public bool Enabled { get; init; }
    public bool SupportsPagination { get; init; }
    public required string RoleEntitlement { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public IReadOnlyList<ProcedureParameterDto> Parameters { get; init; } = [];
    public IReadOnlyList<ProcedureColumnDto> Columns { get; init; } = [];
}
