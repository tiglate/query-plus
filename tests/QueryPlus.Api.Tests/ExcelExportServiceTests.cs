using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;
using QueryPlus.Api.Services;
using QueryPlus.Application.Interfaces;

namespace QueryPlus.Api.Tests;

public class ExcelExportServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IWebHostEnvironment _env = Substitute.For<IWebHostEnvironment>();
    private readonly ExcelExportService _sut;

    public ExcelExportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "QueryPlusTests_" + Guid.NewGuid().ToString("N"));
        _env.ContentRootPath.Returns(_tempDir);
        _sut = new ExcelExportService(_env);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public void QueueExport_EnqueuesJob_And_ReturnsGuid()
    {
        var paramsDict = new Dictionary<string, string?> { ["@Category"] = "Sales" };

        var jobId = _sut.QueueExport(procedureId: 5, paramsDict, username: "test_user", userRoles: ["user"]);

        jobId.Should().NotBeEmpty();

        var job = _sut.GetJob(jobId);
        job.Should().NotBeNull();
        job!.Status.Should().Be(ExportJobStatus.Queued);
        job.Username.Should().Be("test_user");
    }

    [Fact]
    public void GetFilePath_ReturnsNull_WhenJobNotCompleted()
    {
        var jobId = _sut.QueueExport(procedureId: 5, new Dictionary<string, string?>(), username: "user",
            userRoles: ["user"]);

        var path = _sut.GetFilePath(jobId);

        path.Should().BeNull();
    }

    [Fact]
    public void GetFilePath_ReturnsFilePath_WhenJobCompletedAndFileExists()
    {
        var jobId = _sut.QueueExport(procedureId: 5, new Dictionary<string, string?>(), username: "user",
            userRoles: ["user"]);

        _sut.TryGetJobState(jobId, out var state);
        state.Should().NotBeNull();

        var fakeFile = Path.Combine(_sut.ExportDirectory, "test.xlsx");
        File.WriteAllText(fakeFile, "fake content");

        state!.Status = ExportJobStatus.Completed;
        state.FilePath = fakeFile;
        _sut.UpdateJob(state);

        var path = _sut.GetFilePath(jobId);

        path.Should().Be(fakeFile);
    }

    [Fact]
    public void EvictExpiredJobs_RemovesOldCompletedJob_AndDeletesItsFile()
    {
        var jobId = _sut.QueueExport(5, new Dictionary<string, string?>(), "user", userRoles: ["user"]);
        var fakeFile = Path.Combine(_sut.ExportDirectory, "old.xlsx");
        File.WriteAllText(fakeFile, "fake content");
        _sut.TryGetJobState(jobId, out var state);
        state!.Status = ExportJobStatus.Completed;
        state.FilePath = fakeFile;
        state.CompletedAt = DateTime.UtcNow.AddHours(-2);
        _sut.UpdateJob(state);

        var evicted = _sut.EvictExpiredJobs(TimeSpan.FromHours(1));

        evicted.Should().Be(1);
        _sut.GetJob(jobId).Should().BeNull();
        File.Exists(fakeFile).Should().BeFalse();
    }

    [Fact]
    public void EvictExpiredJobs_KeepsRecentCompletedJob()
    {
        var jobId = _sut.QueueExport(5, new Dictionary<string, string?>(), "user", userRoles: ["user"]);
        _sut.TryGetJobState(jobId, out var state);
        state!.Status = ExportJobStatus.Completed;
        state.CompletedAt = DateTime.UtcNow;
        _sut.UpdateJob(state);

        var evicted = _sut.EvictExpiredJobs(TimeSpan.FromHours(1));

        evicted.Should().Be(0);
        _sut.GetJob(jobId).Should().NotBeNull();
    }

    [Fact]
    public void EvictExpiredJobs_NeverEvictsInFlightJobs_RegardlessOfAge()
    {
        var jobId = _sut.QueueExport(5, new Dictionary<string, string?>(), "user", userRoles: ["user"]);
        _sut.TryGetJobState(jobId, out var state);
        state!.Status = ExportJobStatus.Running;
        _sut.UpdateJob(state);
        // CreatedAt is deep in the past relative to the retention window, but Running jobs must
        // never be evicted out from under an in-progress export.
        var oldCreatedState = new ExcelExportService.ExportJobState
        {
            Id = jobId, ProcedureId = 5, Status = ExportJobStatus.Running,
            CreatedAt = DateTime.UtcNow.AddDays(-1), Username = "user"
        };
        _sut.UpdateJob(oldCreatedState);

        var evicted = _sut.EvictExpiredJobs(TimeSpan.FromHours(1));

        evicted.Should().Be(0);
        _sut.GetJob(jobId).Should().NotBeNull();
    }
}
