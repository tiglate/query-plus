using QueryPlus.Domain.Common;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Domain.Entities;

/// <summary>
/// tb_job_definition
/// </summary>
public class JobDefinition : IHasTimestamps, IAuditedEntity
{
    public int IdJobDefinition { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public JobType JobType { get; set; }

    /// <summary>Set exclusively by the script-upload endpoint; null until a script is uploaded.</summary>
    public string? ScriptPath { get; set; }

    /// <summary>Lowercase hex SHA-256, pinned only once the job is approved.</summary>
    public string? ScriptSha256 { get; set; }

    public required string CronExpression { get; set; }
    public required string RunAsUser { get; set; }
    public int MemoryLimitMb { get; set; }
    public int MaxDurationMinutes { get; set; }
    public bool Enabled { get; set; }
    public JobApprovalStatus ApprovalStatus { get; set; }
    public required string CreatedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>Comma-separated notification addresses.</summary>
    public string? NotifyEmails { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<JobRun> JobRuns { get; set; } = new List<JobRun>();
    public ICollection<JobRunRequest> JobRunRequests { get; set; } = new List<JobRunRequest>();
}
