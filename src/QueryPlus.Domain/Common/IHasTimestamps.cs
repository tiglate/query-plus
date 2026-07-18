namespace QueryPlus.Domain.Common;

/// <summary>
/// Entities with created_at / updated_at columns.
/// </summary>
public interface IHasTimestamps
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
