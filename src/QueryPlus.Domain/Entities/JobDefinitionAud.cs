using QueryPlus.Domain.Enums;

namespace QueryPlus.Domain.Entities;

/// <summary>
/// tb_job_definition_aud
/// </summary>
public class JobDefinitionAud
{
    public int IdJobDefinition { get; set; }
    public int IdRevision { get; set; }
    public RevisionTypeCode? IdRevisionType { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? JobType { get; set; }
    public string? ScriptPath { get; set; }
    public string? ScriptSha256 { get; set; }
    public string? CronExpression { get; set; }
    public string? RunAsUser { get; set; }
    public int? MemoryLimitMb { get; set; }
    public int? MaxDurationMinutes { get; set; }
    public bool? Enabled { get; set; }
    public string? ApprovalStatus { get; set; }
    public string? CreatedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? NotifyEmails { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Revision Revision { get; set; } = null!;
    public RevisionType? RevisionType { get; set; }
}
