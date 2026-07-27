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

        var jobId = _sut.QueueExport(procedureId: 5, paramsDict, username: "test_user");

        jobId.Should().NotBeEmpty();

        var job = _sut.GetJob(jobId);
        job.Should().NotBeNull();
        job!.Status.Should().Be(ExportJobStatus.Queued);
        job.Username.Should().Be("test_user");
    }

    [Fact]
    public void GetFilePath_ReturnsNull_WhenJobNotCompleted()
    {
        var jobId = _sut.QueueExport(procedureId: 5, new Dictionary<string, string?>(), username: "user");

        var path = _sut.GetFilePath(jobId);

        path.Should().BeNull();
    }

    [Fact]
    public void GetFilePath_ReturnsFilePath_WhenJobCompletedAndFileExists()
    {
        var jobId = _sut.QueueExport(procedureId: 5, new Dictionary<string, string?>(), username: "user");

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
}
