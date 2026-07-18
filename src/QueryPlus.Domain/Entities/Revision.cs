namespace QueryPlus.Domain.Entities;

/// <summary>
/// tb_revision — one revision header per SaveChanges batch that mutates audited entities.
/// </summary>
public class Revision
{
    public int IdRevision { get; set; }
    public DateTime RevisionTimestamp { get; set; }
    public required string Username { get; set; }
    public string? IpAddress { get; set; }

    public ICollection<CategoryAud> CategoryAudits { get; set; } = new List<CategoryAud>();
    public ICollection<ProcedureAud> ProcedureAudits { get; set; } = new List<ProcedureAud>();
    public ICollection<ProcedureParameterAud> ProcedureParameterAudits { get; set; } = new List<ProcedureParameterAud>();
    public ICollection<ProcedureColumnAud> ProcedureColumnAudits { get; set; } = new List<ProcedureColumnAud>();
}
