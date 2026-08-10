using Cronos;
using FluentValidation;
using Microsoft.Extensions.Options;
using QueryPlus.Application.Common;
using QueryPlus.Application.DTOs.Jobs;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Options;

namespace QueryPlus.Application.Validation;

public sealed class CreateJobDefinitionDtoValidator : AbstractValidator<CreateJobDefinitionDto>
{
    public CreateJobDefinitionDtoValidator(IOptions<JobsOptions> jobsOptions, IJobRunAsUserCatalog runAsUserCatalog)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.JobType).IsInEnum();

        RuleFor(x => x.CronExpression)
            .NotEmpty()
            .MaximumLength(120)
            .Must(BeAValidCronExpression)
            .WithMessage("Cron expression is not valid.");

        RuleFor(x => x.RunAsUser)
            .NotEmpty()
            .MaximumLength(64)
            .Must(JobScriptSecurity.IsValidRunAsUser)
            .WithMessage("Run-as user must be a valid Linux username (letters, digits, underscore, hyphen; " +
                         "cannot start with a digit or hyphen) - it is embedded directly into a cron.d entry.")
            .MustAsync((value, cancellationToken) =>
                IsEligibleRunAsUser(runAsUserCatalog, jobsOptions, value, cancellationToken))
            .WithMessage("Run-as user must be one of the server's eligible non-interactive system accounts.");
        RuleFor(x => x.MemoryLimitMb).GreaterThan(0);
        RuleFor(x => x.MaxDurationMinutes).GreaterThan(0);
        RuleFor(x => x.NotifyEmails).MaximumLength(1000);
    }

    // 5-field standard cron, matching /etc/cron.d syntax - not the 6-field seconds variant.
    internal static bool BeAValidCronExpression(string expression) =>
        CronExpression.TryParse(expression, CronFormat.Standard, out _);

    // Fails CLOSED by default (JobsOptions.EnforceRunAsUserCatalog defaults true): an empty or
    // failed catalog result rejects every value, it does not wave every value through. Treating
    // "the catalog returned nothing" as "can't verify, so allow anything" would mean a transient
    // getent hiccup in production silently disables this module's only defense against
    // designating a privileged/interactive account as a job's run-as user - see
    // JobsOptions.EnforceRunAsUserCatalog's doc comment for the full reasoning. The escape hatch
    // for environments with no real Linux passwd database to query is the config flag itself
    // (appsettings.Development.json sets it false), never an implicit empty-list bypass.
    internal static async Task<bool> IsEligibleRunAsUser(
        IJobRunAsUserCatalog catalog, IOptions<JobsOptions> jobsOptions, string value,
        CancellationToken cancellationToken)
    {
        if (!jobsOptions.Value.EnforceRunAsUserCatalog)
        {
            return true;
        }

        var eligible = await catalog.GetEligibleUsersAsync(cancellationToken);
        return eligible is not null && eligible.Contains(value);
    }
}

public sealed class UpdateJobDefinitionDtoValidator : AbstractValidator<UpdateJobDefinitionDto>
{
    public UpdateJobDefinitionDtoValidator(IOptions<JobsOptions> jobsOptions, IJobRunAsUserCatalog runAsUserCatalog)
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.JobType).IsInEnum();

        RuleFor(x => x.CronExpression)
            .NotEmpty()
            .MaximumLength(120)
            .Must(CreateJobDefinitionDtoValidator.BeAValidCronExpression)
            .WithMessage("Cron expression is not valid.");

        RuleFor(x => x.RunAsUser)
            .NotEmpty()
            .MaximumLength(64)
            .Must(JobScriptSecurity.IsValidRunAsUser)
            .WithMessage("Run-as user must be a valid Linux username (letters, digits, underscore, hyphen; " +
                         "cannot start with a digit or hyphen) - it is embedded directly into a cron.d entry.")
            .MustAsync((value, cancellationToken) => CreateJobDefinitionDtoValidator.IsEligibleRunAsUser(
                runAsUserCatalog, jobsOptions, value, cancellationToken))
            .WithMessage("Run-as user must be one of the server's eligible non-interactive system accounts.");
        RuleFor(x => x.MemoryLimitMb).GreaterThan(0);
        RuleFor(x => x.MaxDurationMinutes).GreaterThan(0);
        RuleFor(x => x.NotifyEmails).MaximumLength(1000);
    }
}

public sealed class RejectJobDefinitionDtoValidator : AbstractValidator<RejectJobDefinitionDto>
{
    public RejectJobDefinitionDtoValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
