using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueryPlus.Application.Common;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.SchedulerSync;

/// <summary>
/// Reconciles /etc/cron.d (or the QUERYPLUS_CRON_D_DIR override, used for local development) so
/// it matches the set of Approved+Enabled job definitions in the database, and drains pending
/// "Run Now" requests via systemd-run. This runs as a single batch pass per invocation - the
/// systemd timer that invokes --sync (about every minute) owns the reconcile cadence, not this
/// class. This is the only privileged component of the Jobs module: in production it runs as
/// root so it can shell out to systemd-run as job.RunAsUser.
/// </summary>
public sealed class CronSyncService(
    IJobDefinitionRepository jobDefinitionRepository,
    IJobRunRequestRepository jobRunRequestRepository,
    IServiceScopeFactory scopeFactory,
    ILogger<CronSyncService> logger)
{
    private const string FilePrefix = "queryplus-job-";

    // "runner" is installed as a sibling of this executable (both live flat under $(PREFIX) - see
    // the Makefile's install-bin target), so AppContext.BaseDirectory is always the right base,
    // regardless of what PREFIX was overridden to. Do not hardcode "/opt/queryplus" here - a
    // custom PREFIX would then install a runner binary that cron/systemd-run can never find.
    private static readonly string RunnerPath = Path.Combine(AppContext.BaseDirectory, "runner");

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var cronDDir = Environment.GetEnvironmentVariable("QUERYPLUS_CRON_D_DIR");
        if (string.IsNullOrWhiteSpace(cronDDir))
        {
            cronDDir = "/etc/cron.d";
        }

        Directory.CreateDirectory(cronDDir);

        var jobs = await jobDefinitionRepository.GetApprovedEnabledAsync(cancellationToken);

        var liveIds = new HashSet<int>(jobs.Select(j => j.IdJobDefinition));

        foreach (var job in jobs)
        {
            try
            {
                WriteCronFile(cronDDir, job);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to reconcile cron.d entry for job {JobId} ({JobName}).",
                    job.IdJobDefinition,
                    job.Name);
            }
        }

        RemoveOrphanFiles(cronDDir, liveIds);

        await DrainRunRequestsAsync(cancellationToken);
    }

    private void WriteCronFile(string cronDDir, JobDefinition job)
    {
        var content = RenderCronLine(job) + "\n";
        var targetPath = Path.Combine(cronDDir, FilePrefix + job.IdJobDefinition);
        var tempPath = Path.Combine(cronDDir, $".{FilePrefix}{job.IdJobDefinition}.tmp");

        if (File.Exists(targetPath))
        {
            var existing = File.ReadAllText(targetPath);
            if (!string.Equals(existing, content, StringComparison.Ordinal))
            {
                logger.LogWarning(
                    "cron.d drift detected for job {JobId} ({JobName}) - overwriting to match database. " +
                    "Previous content of {Path}:\n{OldContent}",
                    job.IdJobDefinition,
                    job.Name,
                    targetPath,
                    existing);
            }
        }

        if (!OperatingSystem.IsWindows())
        {
            // Debian/cronie cron reject group- or world-writable files under /etc/cron.d, and an
            // ambient umask is not a guarantee - set the mode explicitly rather than relying on
            // root's conventional 022. Any line carrying a --setenv= (the OpenBao token, or the DB
            // connection string/password - see RenderCronLine) is locked down to owner-only: cron
            // reads the file as root regardless, but a world-readable 0644 file would otherwise
            // leak whatever secret it carries to any local user. Checking for the generic
            // "--setenv=" marker (rather than naming each secret individually) means this stays
            // correct if another secret is ever propagated this way later, without having to
            // remember to update this check too.
            //
            // The mode is applied via FileStreamOptions.UnixCreateMode so the file is created with
            // it atomically (open(..., O_CREAT, mode)) - setting it with a separate
            // File.SetUnixFileMode call AFTER File.WriteAllText would leave a window where the file
            // exists with default (world-readable) permissions and the secret in it.
            var mode = content.Contains("--setenv=", StringComparison.Ordinal)
                ? UnixFileMode.UserRead | UnixFileMode.UserWrite
                : UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead;

            using var stream = new FileStream(tempPath, new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                UnixCreateMode = mode
            });
            using var writer = new StreamWriter(stream);
            writer.Write(content);
        }
        else
        {
            File.WriteAllText(tempPath, content);
        }

        File.Move(tempPath, targetPath, overwrite: true);
    }

    private string RenderCronLine(JobDefinition job)
    {
        // Defense in depth: CreateJobDefinitionDtoValidator/UpdateJobDefinitionDtoValidator
        // already restrict RunAsUser to a safe username charset, but this line is written
        // verbatim into a root-owned /etc/cron.d file that cron hands to a shell - re-validate
        // here too rather than trusting that every JobDefinition row went through that validator
        // (e.g. a row created before this check existed, or written directly to the database).
        if (!JobScriptSecurity.IsValidRunAsUser(job.RunAsUser))
        {
            throw new InvalidOperationException(
                $"Job {job.IdJobDefinition} has an unsafe RunAsUser value ('{job.RunAsUser}') - refusing to " +
                "render a cron.d entry for it.");
        }

        var fields = new List<string>
        {
            job.CronExpression,
            "root",
            "systemd-run",
            $"--uid={job.RunAsUser}",
            "--scope",
            "-p",
            $"MemoryMax={job.MemoryLimitMb}M",
            $"--description=queryplus-job-{job.IdJobDefinition}"
        };

        AppendPropagatedSecrets(fields, job.IdJobDefinition);

        fields.Add("--");
        fields.Add(RunnerPath);
        fields.Add("--job-definition-id");
        fields.Add(job.IdJobDefinition.ToString());
        fields.Add("--triggered-by");
        fields.Add("schedule");

        return string.Join(' ', fields);
    }

    // Shared by both the cron.d line (RenderCronLine) and the "Run Now" systemd-run invocation
    // (DrainOneRequestAsync) - Runner is launched via `systemd-run --uid=<run_as_user>`, a
    // transient scope, not a persistent systemd unit with its own EnvironmentFile= directive, so
    // there is no privileged mechanism handing it these values automatically the way there is for
    // queryplus-api.service. This process (SchedulerSync) runs as root and can read
    // $(PREFIX)/.env directly, so it reads these once from its own environment and re-supplies
    // them to Runner's child process explicitly.
    private void AppendPropagatedSecrets(List<string> fields, int jobDefinitionId)
    {
        // ConnectionStrings__DefaultConnection is not optional the way OpenBao is: without it
        // Runner cannot reach the QueryPlus catalog database at all (to look up the job, insert
        // its JobRun row, or update status), so it's a hard requirement for the job to run
        // successfully - log clearly if it's missing rather than let Runner fail silently later.
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            fields.Add($"--setenv=ConnectionStrings__DefaultConnection={connectionString}");
        }
        else
        {
            logger.LogError(
                "ConnectionStrings__DefaultConnection is not set in this process's own environment " +
                "(check $(PREFIX)/.env or OpenBao) - job {JobId} will be launched without it and Runner " +
                "will be unable to reach the database.",
                jobDefinitionId);
        }

        var openBaoAddr = Environment.GetEnvironmentVariable("OPENBAO_ADDR");
        var openBaoToken = Environment.GetEnvironmentVariable("OPENBAO_TOKEN");
        if (!string.IsNullOrEmpty(openBaoAddr) && !string.IsNullOrEmpty(openBaoToken))
        {
            // Propagates this process's own OpenBao credentials into the child so Runner can
            // reach OpenBao without a separately distributed credential file. Both-or-neither by
            // design - a half-set pair would send Runner a broken address or a bare token.
            fields.Add($"--setenv=OPENBAO_ADDR={openBaoAddr}");
            fields.Add($"--setenv=OPENBAO_TOKEN={openBaoToken}");
        }
    }

    private void RemoveOrphanFiles(string cronDDir, HashSet<int> liveIds)
    {
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(cronDDir, FilePrefix + "*");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enumerate {Dir} for orphaned cron.d entries.", cronDDir);
            return;
        }

        foreach (var file in files)
        {
            // The dot-prefixed ".queryplus-job-{id}.tmp" temp file never matches this glob (it
            // doesn't start with "queryplus-job-"), so this only ever sees committed per-job files.
            var name = Path.GetFileName(file);
            var idSuffix = name[FilePrefix.Length..];
            var isLive = int.TryParse(idSuffix, out var jobId) && liveIds.Contains(jobId);
            if (isLive)
            {
                continue;
            }

            try
            {
                File.Delete(file);
                logger.LogInformation(
                    "Removed orphaned cron.d entry {File} (job id '{IdSuffix}' is no longer Approved+Enabled, or the name is not a valid job id).",
                    name,
                    idSuffix);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove orphaned cron.d entry {File}.", file);
            }
        }
    }

    private async Task DrainRunRequestsAsync(CancellationToken cancellationToken)
    {
        var requests = await jobRunRequestRepository.GetPendingAsync(cancellationToken);
        if (requests.Count == 0)
        {
            return;
        }

        foreach (var request in requests)
        {
            try
            {
                // A fresh DI scope per request - and therefore a fresh ApplicationDbContext - is
                // required here, not just convenient: IUnitOfWork.SaveChangesAsync does not clear
                // the change tracker on failure, so a save that throws (e.g. a concurrency
                // conflict, or the job row was deleted between the check below and the save)
                // leaves that request's entity tracked as Modified. Sharing one context across the
                // whole loop would mean every subsequent request's SaveChangesAsync in this same
                // pass also fails trying to persist the first request's still-broken change - which
                // would silently suppress ConsumedAt for requests whose systemd-run job already
                // fired, causing the next pass to fire them a second time. Isolating each request
                // in its own scope means one bad save can only ever affect that one request.
                await using var scope = scopeFactory.CreateAsyncScope();
                var scopedJobs = scope.ServiceProvider.GetRequiredService<IJobDefinitionRepository>();
                var scopedRequests = scope.ServiceProvider.GetRequiredService<IJobRunRequestRepository>();
                var scopedUnitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                await DrainOneRequestAsync(scopedJobs, scopedRequests, scopedUnitOfWork, request, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to drain run request {RequestId}.", request.IdJobRunRequest);
            }
        }
    }

    private async Task DrainOneRequestAsync(
        IJobDefinitionRepository jobDefinitionRepository,
        IJobRunRequestRepository jobRunRequestRepository,
        IUnitOfWork unitOfWork,
        JobRunRequest request,
        CancellationToken cancellationToken)
    {
        // RequestRunNowAsync only allows requesting a run for an Approved+Enabled job, but the
        // job may have been disabled/unapproved since the request was queued - re-check
        // defensively rather than trusting the state at request time.
        var job = await jobDefinitionRepository.GetByIdAsync(request.IdJobDefinition, cancellationToken);
        if (job is null || job.ApprovalStatus != JobApprovalStatus.Approved || !job.Enabled)
        {
            logger.LogWarning(
                "Skipping run request {RequestId} for job {JobId} - job is no longer Approved and Enabled.",
                request.IdJobRunRequest,
                request.IdJobDefinition);
            return;
        }

        // Same defense-in-depth charset re-check as RenderCronLine - this value becomes the
        // --uid= argument passed to systemd-run.
        if (!JobScriptSecurity.IsValidRunAsUser(job.RunAsUser))
        {
            logger.LogError(
                "Skipping run request {RequestId} for job {JobId} - unsafe RunAsUser value ('{RunAsUser}').",
                request.IdJobRunRequest,
                request.IdJobDefinition,
                job.RunAsUser);
            return;
        }

        var arguments = new List<string>
        {
            $"--uid={job.RunAsUser}",
            "--scope",
            "-p",
            $"MemoryMax={job.MemoryLimitMb}M",
            $"--description=queryplus-job-{job.IdJobDefinition}-manual"
        };

        AppendPropagatedSecrets(arguments, job.IdJobDefinition);

        arguments.Add("--");
        arguments.Add(RunnerPath);
        arguments.Add("--job-definition-id");
        arguments.Add(job.IdJobDefinition.ToString());
        arguments.Add("--triggered-by");
        arguments.Add("manual");
        arguments.Add("--job-run-request-id");
        arguments.Add(request.IdJobRunRequest.ToString());

        var startInfo = new ProcessStartInfo("systemd-run") { UseShellExecute = false };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // Fire-and-forget: systemd-run itself returns as soon as the transient scope is started,
        // it does not block for the job to finish. We deliberately never WaitForExit() here - a
        // long-running job must not stall this batch pass past the next timer tick.
        using var process = Process.Start(startInfo);
        if (process is null)
        {
            logger.LogError(
                "systemd-run failed to start for run request {RequestId} (job {JobId}).",
                request.IdJobRunRequest,
                request.IdJobDefinition);
            return;
        }

        request.ConsumedAt = DateTime.UtcNow;
        jobRunRequestRepository.Update(request);

        // Known accepted edge case: a crash between the systemd-run call succeeding (above) and
        // this save will cause the next sync pass to re-fire the same request. Acceptable given
        // the no-auto-retry-elsewhere philosophy and the low frequency of manual "Run Now" requests.
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
