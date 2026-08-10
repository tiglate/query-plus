using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Common;
using QueryPlus.Application.Options;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Runner;

/// <summary>
/// The control loop for a single job execution. Invoked once per process - cron/systemd-run
/// launches a fresh QueryPlus.Runner process per scheduled/manual trigger, so all state here is
/// local to this run.
/// </summary>
public sealed class JobRunnerHost(IServiceProvider serviceProvider)
{
    // Fixed absolute interpreter paths, matching the minimal, controlled PATH a systemd-run unit
    // executes under - relying on PATH lookup here would reintroduce exactly the kind of
    // environment-dependent behavior JobScriptSecurity's containment check is meant to rule out.
    private const string BashPath = "/bin/bash";
    private const string PythonInterpreterPath = "/usr/bin/python3";

    /// <summary>Sentinel ExitCode meaning "refused to execute: script hash mismatch".</summary>
    private const int HashMismatchExitCode = -2;

    /// <summary>
    /// Sentinel ExitCode meaning "runner-initiated timeout kill", distinct from any real exit
    /// code the child process itself could produce. JobRunStatus intentionally has no separate
    /// TimedOut value - the run still ends up Failed, with this sentinel carrying the distinction.
    /// </summary>
    private const int TimeoutKillExitCode = -1;

    public async Task<int> RunAsync(RunnerArgs args, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var jobDefinitionRepository = sp.GetRequiredService<IJobDefinitionRepository>();
        var jobRunRepository = sp.GetRequiredService<IJobRunRepository>();
        var jobRunRequestRepository = sp.GetRequiredService<IJobRunRequestRepository>();
        var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
        var jobsOptions = sp.GetRequiredService<IOptions<JobsOptions>>().Value;
        var notificationSender = sp.GetRequiredService<INotificationSender>();
        var logger = sp.GetRequiredService<ILogger<JobRunnerHost>>();

        JobRun? jobRun = null;
        try
        {
            var job = await jobDefinitionRepository.GetByIdAsync(args.JobDefinitionId, cancellationToken);
            if (job is null || job.ApprovalStatus != JobApprovalStatus.Approved || !job.Enabled)
            {
                logger.LogWarning(
                    "Job definition {JobDefinitionId} is not eligible to run right now (found={Found}, approvalStatus={ApprovalStatus}, enabled={Enabled}); skipping.",
                    args.JobDefinitionId, job is not null, job?.ApprovalStatus, job?.Enabled);
                return 0;
            }

            // job.ScriptPath is only null for a job that never had a script uploaded, which can
            // never reach Approved+Enabled (SubmitForApprovalAsync/ApproveAsync both guard against
            // it) - TryResolveContainedPath's null-safe IsNullOrWhiteSpace check covers it anyway
            // if that invariant is ever violated by a row written outside the normal API.
            if (!JobScriptSecurity.TryResolveContainedPath(
                    jobsOptions.ScriptAllowlistRoot, job.ScriptPath!, out var resolvedPath, out var pathError))
            {
                logger.LogError(
                    "Job {JobDefinitionId} ('{JobName}') script path failed the allowlist containment check: {Error}",
                    job.IdJobDefinition, job.Name, pathError);
                return 1;
            }

            var actualSha256 = await JobScriptSecurity.ComputeSha256Async(resolvedPath, cancellationToken);
            if (!JobScriptSecurity.HashMatches(job.ScriptSha256 ?? string.Empty, actualSha256))
            {
                jobRun = await InsertHashMismatchRunAsync(
                    jobRunRepository, unitOfWork, job, args.TriggeredBy, cancellationToken);

                logger.LogError(
                    "Job {JobDefinitionId} ('{JobName}') refused to execute: script hash mismatch (expected {Expected}, actual {Actual}).",
                    job.IdJobDefinition, job.Name, job.ScriptSha256, actualSha256);

                await SendFailureNotificationAsync(
                    notificationSender,
                    job,
                    $"Job '{job.Name}' refused to execute: script hash mismatch (expected {job.ScriptSha256}, actual {actualSha256}). " +
                    "The script on disk no longer matches what was approved.",
                    cancellationToken);

                return 1;
            }

            jobRun = await InsertStartingRunAsync(jobRunRepository, unitOfWork, job, args.TriggeredBy, cancellationToken);

            if (args.JobRunRequestId is { } jobRunRequestId)
            {
                await LinkJobRunRequestAsync(
                    jobRunRequestRepository, unitOfWork, jobRunRequestId, jobRun.IdJobRun, logger, cancellationToken);
            }

            var runDirectory = Path.Combine(jobsOptions.LogRoot, jobRun.IdJobRun.ToString(CultureInfo.InvariantCulture));
            Directory.CreateDirectory(runDirectory);
            var stdoutPath = Path.Combine(runDirectory, "stdout.log");
            var stderrPath = Path.Combine(runDirectory, "stderr.log");

            using var process = StartChildProcess(job.JobType, resolvedPath, stdoutPath, stderrPath);

            jobRun.Status = JobRunStatus.Running;
            jobRun.ChildPid = process.Id;
            jobRun.ChildStartedAtUtc = DateTime.UtcNow;
            jobRun.StartedAt = DateTime.UtcNow;
            jobRun.StdoutPath = stdoutPath;
            jobRun.StderrPath = stderrPath;
            jobRunRepository.Update(jobRun);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            using var heartbeatCts = new CancellationTokenSource();
            var heartbeatTask = RunHeartbeatLoopAsync(
                jobRunRepository, unitOfWork, jobRun.IdJobRun, jobsOptions.HeartbeatIntervalSeconds, logger, heartbeatCts.Token);

            var wasTimeoutKill = await WaitForChildAsync(process, job.MaxDurationMinutes, logger, jobRun.IdJobRun, cancellationToken);

            heartbeatCts.Cancel();
            await heartbeatTask;

            var effectiveExitCode = wasTimeoutKill ? TimeoutKillExitCode : process.ExitCode;
            jobRun.FinishedAt = DateTime.UtcNow;
            jobRun.ExitCode = effectiveExitCode;
            jobRun.Status = effectiveExitCode == 0 ? JobRunStatus.Succeeded : JobRunStatus.Failed;
            jobRunRepository.Update(jobRun);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (jobRun.Status == JobRunStatus.Failed)
            {
                var reason = wasTimeoutKill
                    ? $"it exceeded the {job.MaxDurationMinutes} minute maximum duration and was killed"
                    : $"it exited with code {effectiveExitCode}";
                await SendFailureNotificationAsync(
                    notificationSender,
                    job,
                    $"Job '{job.Name}' run #{jobRun.IdJobRun} failed: {reason}.",
                    cancellationToken);
            }

            return jobRun.Status == JobRunStatus.Succeeded ? 0 : 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception while running job definition {JobDefinitionId}.", args.JobDefinitionId);

            if (jobRun is { IdJobRun: not 0 })
            {
                await TryMarkFailedAsync(jobRunRepository, unitOfWork, jobRun, ex, logger, cancellationToken);
            }

            return 1;
        }
    }

    private static async Task<JobRun> InsertHashMismatchRunAsync(
        IJobRunRepository jobRunRepository,
        IUnitOfWork unitOfWork,
        JobDefinition job,
        JobTriggerSource triggeredBy,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var jobRun = new JobRun
        {
            IdJobDefinition = job.IdJobDefinition,
            Status = JobRunStatus.Failed,
            TriggeredBy = triggeredBy,
            RunnerPid = Environment.ProcessId,
            RunnerStartedAtUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime(),
            HostMachine = Environment.MachineName,
            StartedAt = now,
            FinishedAt = now,
            ExitCode = HashMismatchExitCode,
            CreatedAt = now
        };

        await jobRunRepository.AddAsync(jobRun, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return jobRun;
    }

    private static async Task<JobRun> InsertStartingRunAsync(
        IJobRunRepository jobRunRepository,
        IUnitOfWork unitOfWork,
        JobDefinition job,
        JobTriggerSource triggeredBy,
        CancellationToken cancellationToken)
    {
        var jobRun = new JobRun
        {
            IdJobDefinition = job.IdJobDefinition,
            Status = JobRunStatus.Starting,
            TriggeredBy = triggeredBy,
            RunnerPid = Environment.ProcessId,
            RunnerStartedAtUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime(),
            HostMachine = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };

        await jobRunRepository.AddAsync(jobRun, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return jobRun;
    }

    private static async Task LinkJobRunRequestAsync(
        IJobRunRequestRepository jobRunRequestRepository,
        IUnitOfWork unitOfWork,
        int jobRunRequestId,
        int jobRunId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var request = await jobRunRequestRepository.GetByIdAsync(jobRunRequestId, cancellationToken);
        if (request is null)
        {
            logger.LogWarning(
                "JobRunRequest {JobRunRequestId} referenced by --job-run-request-id was not found; continuing without linking.",
                jobRunRequestId);
            return;
        }

        request.IdJobRun = jobRunId;
        jobRunRequestRepository.Update(request);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static Process StartChildProcess(
        JobType jobType, string resolvedScriptPath, string stdoutPath, string stderrPath)
    {
        var interpreter = jobType switch
        {
            JobType.Shell => BashPath,
            JobType.Python => PythonInterpreterPath,
            _ => throw new InvalidOperationException($"Unsupported job type '{jobType}'.")
        };

        // OS-level redirection via a shell command string, rather than ProcessStartInfo's
        // RedirectStandardOutput/RedirectStandardError, because systemd-run already wraps this
        // whole invocation one layer up and .NET's stdio-redirect pipes have known issues
        // combined with impersonation on some platforms. stdout and stderr are redirected to
        // separate files (not merged via 2>&1) so JobRunService.ReadLogAsync can serve each
        // stream independently, matching what JobRun.StdoutPath/StderrPath promise.
        var shellCommand =
            $"exec {ShellQuote(interpreter)} {ShellQuote(resolvedScriptPath)} " +
            $"> {ShellQuote(stdoutPath)} 2> {ShellQuote(stderrPath)}";

        var startInfo = new ProcessStartInfo
        {
            FileName = BashPath,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(shellCommand);

        var process = new Process { StartInfo = startInfo };
        process.Start();
        return process;
    }

    /// <summary>
    /// Single-quotes <paramref name="value"/>, escaping any embedded single quote, so it is safe
    /// to interpolate into the shell command string built for the child process. Defense in
    /// depth: every value passed here (interpreter path, resolved script path, log path) has
    /// already been validated/derived by trusted code at this point.
    /// </summary>
    private static string ShellQuote(string value)
        => "'" + value.Replace("'", "'\\''") + "'";

    private static async Task<bool> WaitForChildAsync(
        Process process,
        int maxDurationMinutes,
        ILogger logger,
        int jobRunId,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(maxDurationMinutes));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
            return false;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            logger.LogWarning(
                "Job run {JobRunId} exceeded its {MaxDurationMinutes} minute maximum duration; killing child process tree (pid {ChildPid}).",
                jobRunId, maxDurationMinutes, process.Id);

            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to kill timed-out child process {ChildPid} for job run {JobRunId}.", process.Id, jobRunId);
            }

            await process.WaitForExitAsync(CancellationToken.None);
            return true;
        }
    }

    private static async Task RunHeartbeatLoopAsync(
        IJobRunRepository jobRunRepository,
        IUnitOfWork unitOfWork,
        int jobRunId,
        int heartbeatIntervalSeconds,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(heartbeatIntervalSeconds, 1)));
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var run = await jobRunRepository.GetByIdAsync(jobRunId, cancellationToken);
                if (run is null)
                {
                    continue;
                }

                run.LastHeartbeatUtc = DateTime.UtcNow;
                jobRunRepository.Update(run);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected: the heartbeat is cancelled once the child process has exited.
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Heartbeat loop for job run {JobRunId} failed; run will continue without further heartbeats.", jobRunId);
        }
    }

    private static async Task TryMarkFailedAsync(
        IJobRunRepository jobRunRepository,
        IUnitOfWork unitOfWork,
        JobRun jobRun,
        Exception exception,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            jobRun.Status = JobRunStatus.Failed;
            jobRun.FinishedAt = DateTime.UtcNow;
            jobRunRepository.Update(jobRun);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception saveEx)
        {
            logger.LogError(
                saveEx,
                "Failed to mark job run {JobRunId} as Failed after an unhandled exception ({ExceptionMessage}); it may be left stuck Running until the watchdog detects it.",
                jobRun.IdJobRun, exception.Message);
        }
    }

    private static async Task SendFailureNotificationAsync(
        INotificationSender notificationSender,
        JobDefinition job,
        string body,
        CancellationToken cancellationToken)
    {
        var recipients = ParseNotifyEmails(job.NotifyEmails);
        if (recipients.Count == 0)
        {
            return;
        }

        await notificationSender.SendAsync(recipients, $"QueryPlus job failed: {job.Name}", body, cancellationToken);
    }

    private static IReadOnlyCollection<string> ParseNotifyEmails(string? notifyEmails)
    {
        if (string.IsNullOrWhiteSpace(notifyEmails))
        {
            return [];
        }

        return notifyEmails
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
