using QueryPlus.Domain.Common;

namespace QueryPlus.Domain.Entities;

/// <summary>
/// tb_category
/// </summary>
public class Category : IHasTimestamps, IAuditedEntity
{
    public int IdCategory { get; set; }
    public required string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Procedure> Procedures { get; set; } = new List<Procedure>();
}
