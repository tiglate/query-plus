using QueryPlus.Domain.Enums;

namespace QueryPlus.Domain.Entities;

/// <summary>
/// tb_revision_type — INSERT / UPDATE / DELETE lookup.
/// </summary>
public class RevisionType
{
    public RevisionTypeCode IdRevisionType { get; set; }
    public required string Description { get; set; }
}
