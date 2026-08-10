using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Options;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.SchedulerSync;

/// <summary>
/// Detects two failure modes the Jobs module can't self-report from inside a normal run:
/// cron never firing at all ("missed trigger"), and a runner process dying without ever
/// updating a JobRun to a final status ("lost" run). Runs as a single batch pass per
/// invocation - the systemd timer that invokes --watchdog (about every 5 minutes) owns the
/// cadence, not this class.
///
/// Known limitation: this watchdog cannot detect its own non-execution if the host is down or
/// its systemd timer is disabled/masked - that requires external host monitoring and is out of
/// scope for this module.
/// </summary>
public sealed class WatchdogService(
    IJobDefinitionRepository jobDefinitionRepository,
    IJobRunRepository jobRunRepository,
    INotificationSender notificationSender,
    IServiceScopeFactory scopeFactory,
    IOptions<JobsOptions> jobsOptions,
    ILogger<WatchdogService> logger)
{
    /// <summary>How close a JobRun's anchor timestamp must be to an expected cron occurrence (or
    /// vice versa) to count as "this occurrence fired".</summary>
    private static readonly TimeSpan ToleranceWindow = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Soft bound on how many of a job's most recent runs are pulled back (IJobRunRepository has
    /// no window-filtered query, only paged search) to check against expected occurrences. Ample
    /// for the default 60-minute lookback at any sane cron frequency; a job firing so often it
    /// exceeds this within the lookback window is a misconfiguration in its own right.
    /// </summary>
    private const int RunSearchPageSize = 5000;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await CheckMissedTriggersAsync(cancellationToken);
        await CheckLostRunsAsync(cancellationToken);
    }

    private async Task CheckMissedTriggersAsync(CancellationToken cancellationToken)
    {
        var jobs = await jobDefinitionRepository.GetApprovedEnabledAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var lookbackStart = now.AddMinutes(-jobsOptions.Value.WatchdogLookbackMinutes);
        // Occurrences within the tolerance window of "now" haven't had a fair chance to fire and
        // be recorded yet - only evaluate occurrences old enough that a legitimate run would
        // already exist, otherwise every pass would report a false miss for the most recent tick.
        var evaluateUpTo = now - ToleranceWindow;

        foreach (var job in jobs)
        {
            try
            {
                // Fresh scope per job: IUnitOfWork.SaveChangesAsync does not clear the change
                // tracker on failure, so sharing one ApplicationDbContext across every job in this
                // pass would let one job's failed save poison every subsequent job's save in the
                // same pass, silently suppressing missed-trigger detection for the rest of the
                // batch. Isolating per job means one bad save only affects that one job.
                await using var scope = scopeFactory.CreateAsyncScope();
                var scopedJobRuns = scope.ServiceProvider.GetRequiredService<IJobRunRepository>();
                var scopedUnitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                await CheckJobMissedTriggersAsync(
                    scopedJobRuns, scopedUnitOfWork, job, lookbackStart, evaluateUpTo, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to check missed triggers for job {JobId} ({JobName}).",
                    job.IdJobDefinition,
                    job.Name);
            }
        }
    }

    private async Task CheckJobMissedTriggersAsync(
        IJobRunRepository jobRunRepository,
        IUnitOfWork unitOfWork,
        JobDefinition job,
        DateTime lookbackStart,
        DateTime evaluateUpTo,
        CancellationToken cancellationToken)
    {
        // Clamp the window to when the job actually became eligible to fire - otherwise a job
        // approved/enabled mid-window gets a burst of false MissedTrigger rows (and emails) for
        // occurrences that predate its existence as an Approved+Enabled job. ApprovedAt is set at
        // approval time and does not move on a later disable/re-enable toggle, so a re-enabled
        // job can still see a bounded burst covering the disabled period - that residual case is
        // accepted rather than tracking a separate "last enabled" timestamp.
        // DateTime.SpecifyKind is required here: EF returns ApprovedAt/CreatedAt as Kind=Unspecified,
        // and Cronos's UTC GetOccurrences overload throws ArgumentException on non-Utc input.
        var eligibleFromUtc = job.ApprovedAt is { } approvedAt
            ? DateTime.SpecifyKind(approvedAt, DateTimeKind.Utc)
            : DateTime.SpecifyKind(job.CreatedAt, DateTimeKind.Utc);
        var windowStart = eligibleFromUtc > lookbackStart ? eligibleFromUtc : lookbackStart;

        if (evaluateUpTo <= windowStart)
        {
            return;
        }

        if (!CronExpression.TryParse(job.CronExpression, CronFormat.Standard, out var cron))
        {
            logger.LogWarning(
                "Job {JobId} ({JobName}) has an unparseable cron expression '{Cron}' - skipping watchdog check.",
                job.IdJobDefinition,
                job.Name,
                job.CronExpression);
            return;
        }

        var occurrences = cron.GetOccurrences(windowStart, evaluateUpTo, fromInclusive: true, toInclusive: true);

        // No window-filtered repository query exists; pull the job's recent runs once and check
        // every occurrence against the same in-memory list rather than querying per-occurrence.
        var (runs, _) = await jobRunRepository.SearchAsync(
            job.IdJobDefinition,
            status: null,
            page: 1,
            pageSize: RunSearchPageSize,
            cancellationToken);

        var scheduleRuns = runs.Where(r => r.TriggeredBy == JobTriggerSource.Schedule).ToList();

        foreach (var occurrence in occurrences)
        {
            var occurrenceUtc = DateTime.SpecifyKind(occurrence, DateTimeKind.Utc);

            // Includes previously-recorded MissedTrigger rows (TriggeredBy is still Schedule):
            // a MissedTrigger row is anchored to its occurrence via CreatedAt (see
            // RecordMissedTriggerAsync), so this makes the check idempotent across passes -
            // without it, the same occurrence would be re-reported on every subsequent pass.
            var found = scheduleRuns.Any(r => WithinTolerance(r.StartedAt ?? r.CreatedAt, occurrenceUtc));
            if (found)
            {
                continue;
            }

            await RecordMissedTriggerAsync(jobRunRepository, unitOfWork, job, occurrenceUtc, cancellationToken);

            // Keep the in-memory view consistent so a second occurrence in the same pass isn't
            // matched against a MissedTrigger row that doesn't exist in `runs` yet.
            scheduleRuns.Add(new JobRun
            {
                IdJobDefinition = job.IdJobDefinition,
                TriggeredBy = JobTriggerSource.Schedule,
                Status = JobRunStatus.MissedTrigger,
                CreatedAt = occurrenceUtc
            });
        }
    }

    private static bool WithinTolerance(DateTime anchor, DateTime occurrenceUtc)
        => (anchor - occurrenceUtc).Duration() <= ToleranceWindow;

    private async Task RecordMissedTriggerAsync(
        IJobRunRepository jobRunRepository,
        IUnitOfWork unitOfWork,
        JobDefinition job,
        DateTime occurrenceUtc,
        CancellationToken cancellationToken)
    {
        var run = new JobRun
        {
            IdJobDefinition = job.IdJobDefinition,
            Status = JobRunStatus.MissedTrigger,
            TriggeredBy = JobTriggerSource.Schedule,
            // Anchored to the missed occurrence itself (not "now") so the existence check above
            // finds this row on every later pass and never re-reports the same occurrence. This
            // deviates from a literal "CreatedAt = now" reading of the spec but is required for
            // the check to be idempotent - see the comment on the `found` lookup above.
            CreatedAt = occurrenceUtc
        };

        await jobRunRepository.AddAsync(run, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Missed trigger detected for job {JobId} ({JobName}) - expected occurrence at {Occurrence:o} UTC.",
            job.IdJobDefinition,
            job.Name,
            occurrenceUtc);

        await NotifyAsync(
            job,
            $"QueryPlus job missed trigger: {job.Name}",
            $"Job '{job.Name}' (id {job.IdJobDefinition}) was expected to trigger at {occurrenceUtc:o} UTC " +
            "but no scheduled run was recorded for that occurrence.",
            cancellationToken);
    }

    private async Task CheckLostRunsAsync(CancellationToken cancellationToken)
    {
        var activeRuns = await jobRunRepository.GetActiveAsync(cancellationToken);
        var staleThreshold = TimeSpan.FromMinutes(jobsOptions.Value.WatchdogStaleHeartbeatMinutes);
        var now = DateTime.UtcNow;

        foreach (var run in activeRuns)
        {
            try
            {
                // Fresh scope per run, for the same reason as the per-job scope above: isolates
                // one run's failed save from poisoning every other run's save in this pass.
                await using var scope = scopeFactory.CreateAsyncScope();
                var scopedJobRuns = scope.ServiceProvider.GetRequiredService<IJobRunRepository>();
                var scopedJobDefinitions = scope.ServiceProvider.GetRequiredService<IJobDefinitionRepository>();
                var scopedUnitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                await CheckRunLostAsync(
                    scopedJobRuns, scopedJobDefinitions, scopedUnitOfWork, run, now, staleThreshold, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check liveness for run {RunId}.", run.IdJobRun);
            }
        }
    }

    private async Task CheckRunLostAsync(
        IJobRunRepository jobRunRepository,
        IJobDefinitionRepository jobDefinitionRepository,
        IUnitOfWork unitOfWork,
        JobRun run,
        DateTime now,
        TimeSpan staleThreshold,
        CancellationToken cancellationToken)
    {
        var anchor = run.LastHeartbeatUtc ?? run.CreatedAt;
        if (now - anchor < staleThreshold)
        {
            return;
        }

        if (run.RunnerPid is { } pid && Directory.Exists($"/proc/{pid}"))
        {
            // The /proc/{pid} directory existing is inconclusive on its own - the PID could have
            // been reused by an unrelated process since the runner died. A full check would
            // compare the runner's recorded start time against /proc/{pid}/stat's start-time
            // field; that is a documented future enhancement. Treat "inconclusive" as "not lost"
            // to avoid a false-positive Lost verdict from PID reuse.
            return;
        }

        run.Status = JobRunStatus.Lost;
        jobRunRepository.Update(run);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Run {RunId} for job {JobId} marked Lost - runner pid {Pid} is no longer present and the heartbeat " +
            "has been stale since {Anchor:o} UTC.",
            run.IdJobRun,
            run.IdJobDefinition,
            run.RunnerPid,
            anchor);

        var job = await jobDefinitionRepository.GetByIdAsync(run.IdJobDefinition, cancellationToken);
        if (job is null)
        {
            return;
        }

        await NotifyAsync(
            job,
            $"QueryPlus job run lost: {job.Name}",
            $"Run {run.IdJobRun} of job '{job.Name}' (id {job.IdJobDefinition}) appears orchestration-lost: " +
            "the runner process died without recording a final status. " +
            $"Last heartbeat: {(run.LastHeartbeatUtc?.ToString("o") ?? "never")} UTC.",
            cancellationToken);
    }

    private async Task NotifyAsync(JobDefinition job, string subject, string body, CancellationToken cancellationToken)
    {
        var recipients = (job.NotifyEmails ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (recipients.Length == 0)
        {
            return;
        }

        try
        {
            await notificationSender.SendAsync(recipients, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            // Never let a notification failure (e.g. Smtp:Host unset locally) roll back or
            // re-throw past the row that was already saved above - the JobRun state change is
            // the source of truth, the email is best-effort.
            logger.LogError(ex, "Failed to send watchdog notification for job {JobId} ({JobName}).", job.IdJobDefinition, job.Name);
        }
    }
}
