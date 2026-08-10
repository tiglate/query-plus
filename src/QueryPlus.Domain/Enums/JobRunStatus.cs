namespace QueryPlus.Domain.Enums;

/// <summary>
/// Stored in tb_job_run.status (VARCHAR(20)).
/// </summary>
public enum JobRunStatus
{
    Queued = 1,
    Starting = 2,
    Running = 3,
    Succeeded = 4,
    Failed = 5,
    Lost = 6,
    MissedTrigger = 7
}
