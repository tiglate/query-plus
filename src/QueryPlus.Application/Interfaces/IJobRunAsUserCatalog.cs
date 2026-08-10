namespace QueryPlus.Application.Interfaces;

/// <summary>
/// Enumerates OS accounts eligible to be used as a job's RunAsUser: non-root accounts with a
/// non-interactive shell (nologin/false), excluding well-known system/service accounts (see
/// LinuxRunAsUserCatalog.BuiltInDenylist and JobsOptions.DenylistedRunAsUsers) that pass the
/// shell check but were never meant to run arbitrary jobs. Backs both the admin UI dropdown and
/// server-side enforcement in <see cref="QueryPlus.Application.Validation.JobDefinitionValidators"/>.
/// </summary>
public interface IJobRunAsUserCatalog
{
    Task<IReadOnlyList<string>> GetEligibleUsersAsync(CancellationToken cancellationToken = default);
}
