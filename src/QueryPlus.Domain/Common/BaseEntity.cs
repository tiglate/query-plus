namespace QueryPlus.Domain.Common;

/// <summary>
/// Base entity with INT primary key used across the domain model.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}
