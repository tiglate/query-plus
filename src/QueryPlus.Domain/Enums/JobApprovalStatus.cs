namespace QueryPlus.Domain.Enums;

/// <summary>
/// Stored in tb_job_definition.approval_status (VARCHAR(20)).
/// </summary>
public enum JobApprovalStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Rejected = 4
}
