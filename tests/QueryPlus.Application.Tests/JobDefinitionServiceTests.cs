using FluentAssertions;
using NSubstitute;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.DTOs.Jobs;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Options;
using QueryPlus.Application.Services;
using QueryPlus.Application.Validation;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Domain.Interfaces;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace QueryPlus.Application.Tests;

public sealed class JobDefinitionServiceTests : IDisposable
{
    private readonly string _scriptAllowlistRoot;
    private readonly string _logRoot;
    private readonly string _validScriptPath;

    private readonly IJobDefinitionRepository _jobs = Substitute.For<IJobDefinitionRepository>();
    private readonly IJobRunRequestRepository _jobRunRequests = Substitute.For<IJobRunRequestRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();
    private readonly IJobRunAsUserCatalog _runAsUserCatalog = Substitute.For<IJobRunAsUserCatalog>();
    private readonly JobDefinitionService _sut;

    public JobDefinitionServiceTests()
    {
        _scriptAllowlistRoot = Path.Combine(Path.GetTempPath(), "QueryPlusJobServiceTests_Scripts_" + Guid.NewGuid().ToString("N"));
        _logRoot = Path.Combine(Path.GetTempPath(), "QueryPlusJobServiceTests_Logs_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_scriptAllowlistRoot);
        Directory.CreateDirectory(_logRoot);

        _validScriptPath = Path.Combine(_scriptAllowlistRoot, "backup.sh");
        File.WriteAllText(_validScriptPath, "#!/bin/bash\necho hi\n");

        var jobsOptions = MsOptions.Create(new JobsOptions
        {
            ScriptAllowlistRoot = _scriptAllowlistRoot,
            LogRoot = _logRoot
        });

        _currentUser.Username.Returns("approver");
        // "svc-jobs" is the RunAsUser value every DTO/entity builder below uses - the eligible-user
        // check (JobsOptions.EnforceRunAsUserCatalog defaults true, fail-closed) is exercised for
        // real here, not bypassed via an empty catalog.
        _runAsUserCatalog.GetEligibleUsersAsync(Arg.Any<CancellationToken>()).Returns(new[] { "svc-jobs" });

        _sut = new JobDefinitionService(
            _jobs,
            _jobRunRequests,
            _unitOfWork,
            _currentUser,
            jobsOptions,
            new CreateJobDefinitionDtoValidator(jobsOptions, _runAsUserCatalog),
            new UpdateJobDefinitionDtoValidator(jobsOptions, _runAsUserCatalog),
            new RejectJobDefinitionDtoValidator());
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

    private JobDefinition BuildEntity(
        int id = 1,
        string createdBy = "requester",
        JobApprovalStatus approvalStatus = JobApprovalStatus.PendingApproval,
        bool enabled = false,
        bool noScriptPath = false) => new()
    {
        IdJobDefinition = id,
        Name = "Nightly Backup",
        JobType = JobType.Shell,
        ScriptPath = noScriptPath ? null : _validScriptPath,
        CronExpression = "0 2 * * *",
        RunAsUser = "svc-jobs",
        MemoryLimitMb = 512,
        MaxDurationMinutes = 30,
        ApprovalStatus = approvalStatus,
        Enabled = enabled,
        CreatedBy = createdBy
    };

    private CreateJobDefinitionDto ValidCreateDto() => new()
    {
        Name = "Nightly Backup",
        JobType = JobType.Shell,
        CronExpression = "0 2 * * *",
        RunAsUser = "svc-jobs",
        MemoryLimitMb = 512,
        MaxDurationMinutes = 30
    };

    private UpdateJobDefinitionDto ValidUpdateDto(int id) => new()
    {
        Id = id,
        Name = "Nightly Backup",
        JobType = JobType.Shell,
        CronExpression = "0 2 * * *",
        RunAsUser = "svc-jobs",
        MemoryLimitMb = 512,
        MaxDurationMinutes = 30
    };

    // The module's core security invariant: self-approval must be blocked purely by identity
    // comparison, independent of any role check (roles alone can't express "not the requester").
    [Fact]
    public async Task ApproveAsync_CurrentUserEqualsCreatedBy_ThrowsForbidden_EvenWhenPendingApproval()
    {
        var entity = BuildEntity(createdBy: "approver", approvalStatus: JobApprovalStatus.PendingApproval);
        _jobs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);

        Func<Task> act = async () => await _sut.ApproveAsync(1, new ApproveJobDefinitionDto());

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Theory]
    [InlineData(JobApprovalStatus.Draft)]
    [InlineData(JobApprovalStatus.Approved)]
    [InlineData(JobApprovalStatus.Rejected)]
    public async Task ApproveAsync_NotPendingApproval_ThrowsForbidden(JobApprovalStatus status)
    {
        var entity = BuildEntity(createdBy: "requester", approvalStatus: status);
        _jobs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);

        Func<Task> act = async () => await _sut.ApproveAsync(1, new ApproveJobDefinitionDto());

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task ApproveAsync_DifferentUserAndPendingApproval_ApprovesWithoutAutoEnabling()
    {
        var entity = BuildEntity(createdBy: "requester", approvalStatus: JobApprovalStatus.PendingApproval, enabled: false);
        _jobs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);

        var result = await _sut.ApproveAsync(1, new ApproveJobDefinitionDto());

        result.ApprovalStatus.Should().Be(JobApprovalStatus.Approved);
        result.ApprovedBy.Should().Be("approver");
        result.ApprovedAt.Should().NotBeNull();
        result.ScriptSha256.Should().NotBeNullOrEmpty();
        result.Enabled.Should().BeFalse();
        entity.Enabled.Should().BeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ApprovalStatusApproved_ThrowsForbidden()
    {
        var entity = BuildEntity(approvalStatus: JobApprovalStatus.Approved);
        _jobs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);

        Func<Task> act = async () => await _sut.UpdateAsync(1, ValidUpdateDto(1));

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Theory]
    [InlineData(JobApprovalStatus.Draft)]
    [InlineData(JobApprovalStatus.Rejected)]
    public async Task UpdateAsync_DraftOrRejected_Succeeds(JobApprovalStatus status)
    {
        var entity = BuildEntity(approvalStatus: status);
        _jobs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);
        _jobs.ExistsByNameAsync("Nightly Backup", 1, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.UpdateAsync(1, ValidUpdateDto(1));

        result.Name.Should().Be("Nightly Backup");
        _jobs.Received(1).Update(entity);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestRunNowAsync_NotApprovedOrNotEnabled_ThrowsForbidden()
    {
        var entity = BuildEntity(approvalStatus: JobApprovalStatus.Approved, enabled: false);
        _jobs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);

        Func<Task> act = async () => await _sut.RequestRunNowAsync(1);

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task RequestRunNowAsync_ApprovedAndEnabled_CreatesRunRequest()
    {
        var entity = BuildEntity(approvalStatus: JobApprovalStatus.Approved, enabled: true);
        _jobs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);
        _currentUser.Username.Returns("requester");

        var result = await _sut.RequestRunNowAsync(1);

        result.JobDefinitionId.Should().Be(1);
        result.RequestedBy.Should().Be("requester");
        await _jobRunRequests.Received(1).AddAsync(
            Arg.Is<JobRunRequest>(r => r != null && r.IdJobDefinition == 1 && r.RequestedBy == "requester"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectAsync_PendingApproval_SetsRejectedAndPersistsReason()
    {
        var entity = BuildEntity(approvalStatus: JobApprovalStatus.PendingApproval);
        _jobs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);

        var result = await _sut.RejectAsync(1, new RejectJobDefinitionDto { Reason = "Needs review" });

        result.ApprovalStatus.Should().Be(JobApprovalStatus.Rejected);
        result.RejectionReason.Should().Be("Needs review");
        entity.ApprovalStatus.Should().Be(JobApprovalStatus.Rejected);
        entity.RejectionReason.Should().Be("Needs review");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsValidationException()
    {
        _jobs.ExistsByNameAsync("Nightly Backup", null, Arg.Any<CancellationToken>()).Returns(true);

        Func<Task> act = async () => await _sut.CreateAsync(ValidCreateDto());

        var exc = await act.Should().ThrowAsync<Common.ValidationException>();
        exc.Which.Errors.Should().ContainKey("Name");
        exc.Which.Errors["Name"].Should().Contain(m => m.Contains("already exists"));
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesJobInDraftStatus()
    {
        _jobs.ExistsByNameAsync("Nightly Backup", null, Arg.Any<CancellationToken>()).Returns(false);
        _currentUser.Username.Returns("requester");

        var result = await _sut.CreateAsync(ValidCreateDto());

        result.Name.Should().Be("Nightly Backup");
        result.ApprovalStatus.Should().Be(JobApprovalStatus.Draft);
        result.CreatedBy.Should().Be("requester");
        result.Enabled.Should().BeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitForApprovalAsync_NoScriptUploaded_ThrowsValidationException()
    {
        var entity = BuildEntity(approvalStatus: JobApprovalStatus.Draft, noScriptPath: true);
        _jobs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);

        Func<Task> act = async () => await _sut.SubmitForApprovalAsync(1);

        var exc = await act.Should().ThrowAsync<Common.ValidationException>();
        exc.Which.Errors.Should().ContainKey("ScriptPath");
    }

    [Fact]
    public async Task ApproveAsync_NoScriptUploaded_ThrowsValidationException()
    {
        var entity = BuildEntity(
            createdBy: "requester", approvalStatus: JobApprovalStatus.PendingApproval, noScriptPath: true);
        _jobs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);

        Func<Task> act = async () => await _sut.ApproveAsync(1, new ApproveJobDefinitionDto());

        var exc = await act.Should().ThrowAsync<Common.ValidationException>();
        exc.Which.Errors.Should().ContainKey("ScriptPath");
    }

    [Fact]
    public async Task UploadScriptAsync_WrongExtensionForJobType_ThrowsValidationException()
    {
        var entity = BuildEntity(approvalStatus: JobApprovalStatus.Draft, noScriptPath: true);
        entity.JobType = JobType.Shell;
        _jobs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);

        using var content = new MemoryStream("print('hi')"u8.ToArray());
        Func<Task> act = async () => await _sut.UploadScriptAsync(1, content, "script.py", content.Length);

        var exc = await act.Should().ThrowAsync<Common.ValidationException>();
        exc.Which.Errors.Should().ContainKey("originalFileName");
    }

    [Fact]
    public async Task UploadScriptAsync_ExceedsMaxSize_ThrowsValidationException()
    {
        var jobsOptions = MsOptions.Create(new JobsOptions
        {
            ScriptAllowlistRoot = _scriptAllowlistRoot,
            LogRoot = _logRoot,
            MaxScriptUploadBytes = 10
        });
        var sut = new JobDefinitionService(
            _jobs,
            _jobRunRequests,
            _unitOfWork,
            _currentUser,
            jobsOptions,
            new CreateJobDefinitionDtoValidator(jobsOptions, _runAsUserCatalog),
            new UpdateJobDefinitionDtoValidator(jobsOptions, _runAsUserCatalog),
            new RejectJobDefinitionDtoValidator());

        var entity = BuildEntity(approvalStatus: JobApprovalStatus.Draft, noScriptPath: true);
        entity.JobType = JobType.Shell;
        _jobs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);

        using var content = new MemoryStream("#!/bin/bash\necho this is way too long for the limit\n"u8.ToArray());
        Func<Task> act = async () => await sut.UploadScriptAsync(1, content, "script.sh", content.Length);

        var exc = await act.Should().ThrowAsync<Common.ValidationException>();
        exc.Which.Errors.Should().ContainKey("contentLength");
    }

    [Fact]
    public async Task UploadScriptAsync_ValidShellScript_StoresAtConventionalPathAndSetsScriptPath()
    {
        var entity = BuildEntity(id: 5, approvalStatus: JobApprovalStatus.Draft, noScriptPath: true);
        entity.JobType = JobType.Shell;
        _jobs.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(entity);

        using var content = new MemoryStream("#!/bin/bash\necho hi\n"u8.ToArray());
        var result = await _sut.UploadScriptAsync(5, content, "backup.sh", content.Length);

        var expectedPath = Path.Combine(_scriptAllowlistRoot, "uploads", "job-5", "run.sh");
        result.ScriptPath.Should().Be(expectedPath);
        File.Exists(expectedPath).Should().BeTrue();
        entity.ScriptPath.Should().Be(expectedPath);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
