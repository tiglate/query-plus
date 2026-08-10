namespace QueryPlus.Domain.Enums;

/// <summary>
/// Stored in tb_job_run.triggered_by (VARCHAR(20)).
/// </summary>
public enum JobTriggerSource
{
    Schedule = 1,
    Manual = 2
}
