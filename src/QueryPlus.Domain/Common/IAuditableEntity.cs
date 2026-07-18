namespace QueryPlus.Domain.Common;

/// <summary>
/// Marker for entities that track creation and modification timestamps.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
