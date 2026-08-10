using FluentAssertions;
using NSubstitute;
using QueryPlus.Application.DTOs.Jobs;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Options;
using QueryPlus.Application.Validation;
using QueryPlus.Domain.Enums;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace QueryPlus.Application.Tests;

public sealed class JobDefinitionValidatorsTests : IDisposable
{
    private readonly string _scriptAllowlistRoot;
    private readonly string _logRoot;
    private readonly IJobRunAsUserCatalog _runAsUserCatalog = Substitute.For<IJobRunAsUserCatalog>();
    private readonly CreateJobDefinitionDtoValidator _createValidator;
    private readonly UpdateJobDefinitionDtoValidator _updateValidator;

    public JobDefinitionValidatorsTests()
    {
        _scriptAllowlistRoot = Path.Combine(Path.GetTempPath(), "QueryPlusJobValidatorTests_Scripts_" + Guid.NewGuid().ToString("N"));
        _logRoot = Path.Combine(Path.GetTempPath(), "QueryPlusJobValidatorTests_Logs_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_scriptAllowlistRoot);
        Directory.CreateDirectory(_logRoot);

        var jobsOptions = MsOptions.Create(new JobsOptions
        {
            ScriptAllowlistRoot = _scriptAllowlistRoot,
            LogRoot = _logRoot
        });

        // "svc-jobs" is the RunAsUser value ValidCreateDto()/the Update test DTO use - the
        // eligible-user check (JobsOptions.EnforceRunAsUserCatalog defaults true, fail-closed) is
        // exercised for real here, not bypassed via an empty catalog. Individual tests below
        // override this per-case to specifically exercise the eligible/not-eligible branches.
        _runAsUserCatalog.GetEligibleUsersAsync(Arg.Any<CancellationToken>()).Returns(new[] { "svc-jobs" });

        _createValidator = new CreateJobDefinitionDtoValidator(jobsOptions, _runAsUserCatalog);
        _updateValidator = new UpdateJobDefinitionDtoValidator(jobsOptions, _runAsUserCatalog);
    }

    public void Dispose()
    {
        foreach (var dir in new[] { _scriptAllowlistRoot, _logRoot })
        {
            if (Directory.Exists(dir))
            {
                try { Directory.Delete(dir, recursive: true); } catch { }
            }
        }
    }

    private static CreateJobDefinitionDto ValidCreateDto(string? cronExpression = "* * * * *") => new()
    {
        Name = "Nightly Backup",
        Description = "Backs up the warehouse",
        JobType = JobType.Shell,
        CronExpression = cronExpression!,
        RunAsUser = "svc-jobs",
        MemoryLimitMb = 512,
        MaxDurationMinutes = 30,
        NotifyEmails = "ops@example.com"
    };

    [Theory]
    [InlineData("* * * * *")] // every minute
    [InlineData("0 2 * * *")] // 2am daily
    public async Task CreateJobDefinitionDtoValidator_ValidFiveFieldCron_Accepts(string cron)
    {
        var result = await _createValidator.ValidateAsync(ValidCreateDto(cron));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("not a cron expression")]
    [InlineData("* * * *")] // too few fields (4)
    public async Task CreateJobDefinitionDtoValidator_InvalidCron_RejectsWithCronExpressionError(string cron)
    {
        var result = await _createValidator.ValidateAsync(ValidCreateDto(cron));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateJobDefinitionDto.CronExpression));
    }

    [Fact]
    public async Task CreateJobDefinitionDtoValidator_SixFieldSecondsVariantCron_Rejects()
    {
        var result = await _createValidator.ValidateAsync(ValidCreateDto("0 0 2 * * *"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateJobDefinitionDto.CronExpression));
    }

    [Fact]
    public async Task CreateJobDefinitionDtoValidator_EmptyName_Rejects()
    {
        var dto = ValidCreateDto();
        var invalid = new CreateJobDefinitionDto
        {
            Name = "",
            Description = dto.Description,
            JobType = dto.JobType,
            CronExpression = dto.CronExpression,
            RunAsUser = dto.RunAsUser,
            MemoryLimitMb = dto.MemoryLimitMb,
            MaxDurationMinutes = dto.MaxDurationMinutes,
            NotifyEmails = dto.NotifyEmails
        };

        var result = await _createValidator.ValidateAsync(invalid);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateJobDefinitionDto.Name));
    }

    [Fact]
    public async Task CreateJobDefinitionDtoValidator_NameTooLong_Rejects()
    {
        var dto = ValidCreateDto();
        var invalid = new CreateJobDefinitionDto
        {
            Name = new string('x', 201),
            Description = dto.Description,
            JobType = dto.JobType,
            CronExpression = dto.CronExpression,
            RunAsUser = dto.RunAsUser,
            MemoryLimitMb = dto.MemoryLimitMb,
            MaxDurationMinutes = dto.MaxDurationMinutes,
            NotifyEmails = dto.NotifyEmails
        };

        var result = await _createValidator.ValidateAsync(invalid);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateJobDefinitionDto.Name));
    }

    [Fact]
    public async Task CreateJobDefinitionDtoValidator_ValidDto_Accepts()
    {
        var result = await _createValidator.ValidateAsync(ValidCreateDto());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateJobDefinitionDtoValidator_RunAsUserNotInNonEmptyCatalog_Rejects()
    {
        _runAsUserCatalog.GetEligibleUsersAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { "svc-allowed" });

        var result = await _createValidator.ValidateAsync(ValidCreateDto());

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateJobDefinitionDto.RunAsUser));
    }

    [Fact]
    public async Task CreateJobDefinitionDtoValidator_RunAsUserInNonEmptyCatalog_Accepts()
    {
        _runAsUserCatalog.GetEligibleUsersAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { "svc-jobs" });

        var result = await _createValidator.ValidateAsync(ValidCreateDto());

        result.IsValid.Should().BeTrue();
    }

    // The security-critical regression: an empty/unavailable catalog must REJECT, not silently
    // allow every value (which would let RunAsUser=root through when getent is unavailable).
    [Fact]
    public async Task CreateJobDefinitionDtoValidator_EmptyCatalogWithEnforcementOn_Rejects()
    {
        _runAsUserCatalog.GetEligibleUsersAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        var result = await _createValidator.ValidateAsync(ValidCreateDto());

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateJobDefinitionDto.RunAsUser));
    }

    [Fact]
    public async Task CreateJobDefinitionDtoValidator_EmptyCatalogWithEnforcementOff_Accepts()
    {
        var jobsOptions = MsOptions.Create(new JobsOptions
        {
            ScriptAllowlistRoot = _scriptAllowlistRoot,
            LogRoot = _logRoot,
            EnforceRunAsUserCatalog = false
        });
        var validator = new CreateJobDefinitionDtoValidator(jobsOptions, _runAsUserCatalog);
        _runAsUserCatalog.GetEligibleUsersAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        var result = await validator.ValidateAsync(ValidCreateDto());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateJobDefinitionDtoValidator_ValidFiveFieldCron_Accepts()
    {
        var dto = new UpdateJobDefinitionDto
        {
            Id = 1,
            Name = "Nightly Backup",
            Description = "Backs up the warehouse",
            JobType = JobType.Shell,
            CronExpression = "0 2 * * *",
            RunAsUser = "svc-jobs",
            MemoryLimitMb = 512,
            MaxDurationMinutes = 30,
            NotifyEmails = "ops@example.com"
        };

        var result = await _updateValidator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateJobDefinitionDtoValidator_SixFieldSecondsVariantCron_Rejects()
    {
        var dto = new UpdateJobDefinitionDto
        {
            Id = 1,
            Name = "Nightly Backup",
            JobType = JobType.Shell,
            CronExpression = "0 0 2 * * *",
            RunAsUser = "svc-jobs",
            MemoryLimitMb = 512,
            MaxDurationMinutes = 30
        };

        var result = await _updateValidator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateJobDefinitionDto.CronExpression));
    }
}
