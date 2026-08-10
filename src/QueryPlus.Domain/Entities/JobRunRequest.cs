namespace QueryPlus.Domain.Entities;

/// <summary>
/// tb_job_run_request - "Run Now" queue, drained by QueryPlus.SchedulerSync so the API process
/// never itself launches the privileged systemd-run invocation.
/// </summary>
public class JobRunRequest
{
    public int IdJobRunRequest { get; set; }
    public int IdJobDefinition { get; set; }
    public required string RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ConsumedAt { get; set; }

    /// <summary>Set by the runner once it inserts its own JobRun row for this request.</summary>
    public int? IdJobRun { get; set; }

    public JobDefinition JobDefinition { get; set; } = null!;
    public JobRun? JobRun { get; set; }
}
