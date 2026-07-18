using QueryPlus.Domain.Enums;

namespace QueryPlus.Domain.Entities;

/// <summary>
/// tb_category_aud
/// </summary>
public class CategoryAud
{
    public int IdCategory { get; set; }
    public int IdRevision { get; set; }
    public RevisionTypeCode? IdRevisionType { get; set; }
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Revision Revision { get; set; } = null!;
    public RevisionType? RevisionType { get; set; }
}
