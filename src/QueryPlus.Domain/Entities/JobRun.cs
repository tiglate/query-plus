using QueryPlus.Domain.Enums;

namespace QueryPlus.Domain.Entities;

/// <summary>
/// tb_job_run - operational execution history, not an approval-gated catalog entity, so it is
/// not audited (same treatment as ExecutionLog).
/// </summary>
public class JobRun
{
    public int IdJobRun { get; set; }
    public int IdJobDefinition { get; set; }
    public JobRunStatus Status { get; set; }
    public JobTriggerSource TriggeredBy { get; set; }
    public int? RunnerPid { get; set; }

    /// <summary>The runner process's own start time - combined with RunnerPid this guards against PID reuse when the watchdog checks liveness.</summary>
    public DateTime? RunnerStartedAtUtc { get; set; }

    public int? ChildPid { get; set; }
    public DateTime? ChildStartedAtUtc { get; set; }
    public DateTime? LastHeartbeatUtc { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public int? ExitCode { get; set; }
    public string? StdoutPath { get; set; }
    public string? StderrPath { get; set; }
    public string? HostMachine { get; set; }
    public DateTime CreatedAt { get; set; }

    public JobDefinition JobDefinition { get; set; } = null!;
}
