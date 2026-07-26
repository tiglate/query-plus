namespace QueryPlus.Application.DTOs.Procedures;

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
