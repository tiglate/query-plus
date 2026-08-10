namespace QueryPlus.Application.Options;

/// <summary>
/// Config-driven (appsettings/OpenBao), not DB-editable - keeps the script allowlist root and
/// log directory a trusted, deployment-fixed boundary a ROLE_JOB_WRITE user can never influence.
/// Bound identically by QueryPlus.Api (for the log-tail endpoint), QueryPlus.Runner (writes logs,
/// executes scripts) and QueryPlus.SchedulerSync (renders cron.d entries).
/// </summary>
public sealed class JobsOptions
{
    public const string SectionName = "Jobs";

    /// <summary>Absolute path. Job scripts must resolve inside this directory.</summary>
    public required string ScriptAllowlistRoot { get; init; }

    /// <summary>
    /// Absolute path shared identically by the API (serving log tails) and Runner (writing logs) -
    /// Runner has no IWebHostEnvironment.ContentRootPath, so this cannot be contentroot-relative
    /// the way ExcelExportService's App_Data/exports convention is.
    /// </summary>
    public required string LogRoot { get; init; }

    public int HeartbeatIntervalSeconds { get; init; } = 20;
    public int WatchdogLookbackMinutes { get; init; } = 60;
    public int WatchdogStaleHeartbeatMinutes { get; init; } = 5;

    /// <summary>Hard cap on an uploaded job script's size, in bytes. Default 1 MiB.</summary>
    public long MaxScriptUploadBytes { get; init; } = 1_048_576;

    /// <summary>
    /// Defaults true (secure/production behavior): CreateJobDefinitionDtoValidator/
    /// UpdateJobDefinitionDtoValidator reject any RunAsUser not present in
    /// IJobRunAsUserCatalog.GetEligibleUsersAsync's result, INCLUDING an empty result - an empty
    /// catalog is never treated as "can't verify, so allow anything," since on real Linux hosts an
    /// empty result means either no eligible account currently exists (a fact, not a reason to
    /// bypass the check) or the catalog query itself failed (getent misbehaving, an NSS hiccup) -
    /// silently opening the door to any charset-valid username there would defeat the point of
    /// having the catalog at all. Set to false ONLY for local/dev/test environments that have no
    /// real Linux passwd database to query (appsettings.Development.json already does this) - never
    /// in production.
    /// </summary>
    public bool EnforceRunAsUserCatalog { get; init; } = true;

    /// <summary>
    /// Extra account names to exclude from IJobRunAsUserCatalog.GetEligibleUsersAsync's result, on
    /// top of LinuxRunAsUserCatalog.BuiltInDenylist (which already covers the common Debian/Ubuntu
    /// default service accounts - _apt, daemon, www-data, nobody, etc.). Non-interactive shell
    /// alone doesn't mean "safe to run a job as": most base-install and packaged system service
    /// accounts (postgres, redis, a distro's own daemon accounts, ...) also use nologin/false, but
    /// letting a job run as one grants it whatever that service's own file/socket access is. Add
    /// site-specific accounts here (e.g. a database service account this box happens to run)
    /// instead of trying to keep BuiltInDenylist exhaustive for every possible package.
    /// </summary>
    public IReadOnlyCollection<string> DenylistedRunAsUsers { get; init; } = [];
}
