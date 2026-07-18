using QueryPlus.Domain.Common;

namespace QueryPlus.Domain.Entities;

/// <summary>
/// Represents a saved query / stored procedure definition that can be executed dynamically.
/// </summary>
public class QueryDefinition : BaseEntity, IAuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string StoredProcedureName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
