using System.Data;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QueryPlus.Api.Services;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Interfaces;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Api.Tests;

/// <summary>
/// Regression coverage for the export worker's stale-entitlement re-check: a job queued while
/// the caller held a role that later gets revoked (or was queued via a route that doesn't
/// recompute entitlement) must fail instead of silently exporting data the caller can no
/// longer see interactively.
/// </summary>
public sealed class ExcelExportBackgroundServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IProcedureRepository _procedureRepository = Substitute.For<IProcedureRepository>();
    private readonly IExecutionRepository _executionRepository = Substitute.For<IExecutionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IStoredProcedureExecutor _executor = Substitute.For<IStoredProcedureExecutor>();
    private readonly ExcelExportService _exports;
    private readonly ExcelExportBackgroundService _sut;

    public ExcelExportBackgroundServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "QueryPlusTests_" + Guid.NewGuid().ToString("N"));
        var env = Substitute.For<IWebHostEnvironment>();
        env.ContentRootPath.Returns(_tempDir);
        _exports = new ExcelExportService(env);

        var services = new ServiceCollection();
        services.AddSingleton(_procedureRepository);
        services.AddSingleton(_executionRepository);
        services.AddSingleton(_unitOfWork);
        services.AddSingleton(_executor);
        var provider = services.BuildServiceProvider();

        _sut = new ExcelExportBackgroundService(_exports, provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ExcelExportBackgroundService>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task Job_fails_when_the_captured_roles_no_longer_satisfy_the_procedures_entitlement()
    {
        var procedure = new Procedure
        {
            IdProcedure = 30, IdCategory = 1, Caption = "Payroll Export", ConnectionName = "DefaultConnection", DatabaseName = "db",
            ProcedureName = "dbo.usp_Payroll", Enabled = true, RoleEntitlement = "ROLE_FINANCE",
            Parameters = [], Columns = []
        };
        _procedureRepository.GetEnabledByIdWithDetailsAsync(30, Arg.Any<CancellationToken>()).Returns(procedure);

        var jobId = _exports.QueueExport(30, new Dictionary<string, string?>(), "someone",
            userRoles: ["ROLE_QUERY_EXEC"]); // does not include ROLE_FINANCE

        await _sut.StartAsync(CancellationToken.None);
        await WaitForTerminalStatusAsync(jobId);
        await _sut.StopAsync(CancellationToken.None);

        var job = _exports.GetJob(jobId);
        job.Should().NotBeNull();
        job!.Status.Should().Be(ExportJobStatus.Failed);
        job.ErrorMessage.Should().Contain("no longer entitled");
        await _executor.DidNotReceive().ExecuteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<IReadOnlyCollection<string>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Job_completes_and_writes_a_readable_workbook_when_entitled()
    {
        var procedure = new Procedure
        {
            IdProcedure = 31, IdCategory = 1, Caption = "Public Export", ConnectionName = "DefaultConnection", DatabaseName = "db",
            ProcedureName = "dbo.usp_Public", Enabled = true, RoleEntitlement = "",
            Parameters = [], Columns = []
        };
        _procedureRepository.GetEnabledByIdWithDetailsAsync(31, Arg.Any<CancellationToken>()).Returns(procedure);

        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Active", typeof(bool));
        table.Rows.Add(1, "Alpha", true);
        table.Rows.Add(2, DBNull.Value, false);
        _executor.ExecuteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, object?>>(),
                Arg.Any<IReadOnlyCollection<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new StoredProcedureExecutionResult { Data = table });

        var jobId = _exports.QueueExport(31, new Dictionary<string, string?>(), "someone", userRoles: []);

        await _sut.StartAsync(CancellationToken.None);
        await WaitForTerminalStatusAsync(jobId);
        await _sut.StopAsync(CancellationToken.None);

        var job = _exports.GetJob(jobId);
        job.Should().NotBeNull();
        job!.Status.Should().Be(ExportJobStatus.Completed);
        job.RowCount.Should().Be(2);

        var filePath = _exports.GetFilePath(jobId);
        filePath.Should().NotBeNull();
        using var workbook = new XLWorkbook(filePath!);
        var sheet = workbook.Worksheet("Results");
        sheet.Cell(1, 1).GetString().Should().Be("Id");
        sheet.Cell(1, 2).GetString().Should().Be("Name");
        sheet.Cell(2, 1).GetDouble().Should().Be(1);
        sheet.Cell(2, 2).GetString().Should().Be("Alpha");
        sheet.Cell(3, 2).IsEmpty().Should().BeTrue();
    }

    [Fact]
    public async Task Job_fails_withOriginalExceptionMessage_when_executor_throws()
    {
        var procedure = new Procedure
        {
            IdProcedure = 32, IdCategory = 1, Caption = "Broken Export", ConnectionName = "DefaultConnection", DatabaseName = "db",
            ProcedureName = "dbo.usp_Broken", Enabled = true, RoleEntitlement = "",
            Parameters = [], Columns = []
        };
        _procedureRepository.GetEnabledByIdWithDetailsAsync(32, Arg.Any<CancellationToken>()).Returns(procedure);
        _executor.ExecuteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, object?>>(),
                Arg.Any<IReadOnlyCollection<string>?>(), Arg.Any<CancellationToken>())
            .Returns<Task<StoredProcedureExecutionResult>>(_ => throw new InvalidOperationException("boom"));

        var jobId = _exports.QueueExport(32, new Dictionary<string, string?>(), "someone", userRoles: []);

        await _sut.StartAsync(CancellationToken.None);
        await WaitForTerminalStatusAsync(jobId);
        await _sut.StopAsync(CancellationToken.None);

        var job = _exports.GetJob(jobId);
        job.Should().NotBeNull();
        job!.Status.Should().Be(ExportJobStatus.Failed);
        job.ErrorMessage.Should().Be("boom");
    }

    [Fact]
    public async Task Job_usesTotalRecords_ForExecutionLogRowCount_WhenProcedureSupportsPagination()
    {
        var procedure = new Procedure
        {
            IdProcedure = 33, IdCategory = 1, Caption = "Paged Export", ConnectionName = "DefaultConnection", DatabaseName = "db",
            ProcedureName = "dbo.usp_Paged", Enabled = true, RoleEntitlement = "", SupportsPagination = true,
            Parameters = [], Columns = []
        };
        _procedureRepository.GetEnabledByIdWithDetailsAsync(33, Arg.Any<CancellationToken>()).Returns(procedure);

        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Rows.Add(1); // only 1 row returned in this page, but 500 total records exist
        _executor.ExecuteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, object?>>(),
                Arg.Any<IReadOnlyCollection<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new StoredProcedureExecutionResult { Data = table, TotalRecords = 500 });

        ExecutionLog? capturedLog = null;
        await _executionRepository.AddAsync(
            Arg.Do<ExecutionLog>(l => capturedLog = l), Arg.Any<CancellationToken>());

        var jobId = _exports.QueueExport(33, new Dictionary<string, string?>(), "someone", userRoles: []);

        await _sut.StartAsync(CancellationToken.None);
        await WaitForTerminalStatusAsync(jobId);
        await _sut.StopAsync(CancellationToken.None);

        var job = _exports.GetJob(jobId);
        job!.Status.Should().Be(ExportJobStatus.Completed);
        job.RowCount.Should().Be(1); // the job's own RowCount always reflects rows actually written to the sheet
        capturedLog.Should().NotBeNull();
        capturedLog!.RowCount.Should().Be(500); // but the audit log records the true total, not just this page
    }

    [Fact]
    public async Task Job_writesExpectedCellRepresentations_ForNonPrimitiveColumnTypes()
    {
        var procedure = new Procedure
        {
            IdProcedure = 34, IdCategory = 1, Caption = "Mixed Types Export", ConnectionName = "DefaultConnection", DatabaseName = "db",
            ProcedureName = "dbo.usp_MixedTypes", Enabled = true, RoleEntitlement = "",
            Parameters = [], Columns = []
        };
        _procedureRepository.GetEnabledByIdWithDetailsAsync(34, Arg.Any<CancellationToken>()).Returns(procedure);

        var table = new DataTable();
        table.Columns.Add("Started", typeof(DateTimeOffset));
        table.Columns.Add("Duration", typeof(TimeSpan));
        table.Columns.Add("Blob", typeof(byte[]));
        var startedAt = new DateTimeOffset(2026, 3, 5, 10, 30, 0, TimeSpan.Zero);
        var duration = TimeSpan.FromMinutes(90);
        var blob = new byte[] { 1, 2, 3 };
        table.Rows.Add(startedAt, duration, blob);
        _executor.ExecuteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, object?>>(),
                Arg.Any<IReadOnlyCollection<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new StoredProcedureExecutionResult { Data = table });

        var jobId = _exports.QueueExport(34, new Dictionary<string, string?>(), "someone", userRoles: []);

        await _sut.StartAsync(CancellationToken.None);
        await WaitForTerminalStatusAsync(jobId);
        await _sut.StopAsync(CancellationToken.None);

        var filePath = _exports.GetFilePath(jobId);
        filePath.Should().NotBeNull();
        using var workbook = new XLWorkbook(filePath!);
        var sheet = workbook.Worksheet("Results");
        sheet.Cell(2, 1).GetDateTime().Should().Be(startedAt.DateTime);
        sheet.Cell(2, 2).GetTimeSpan().Should().Be(duration);
        sheet.Cell(2, 3).GetString().Should().Be(Convert.ToBase64String(blob));
    }

    private async Task WaitForTerminalStatusAsync(Guid jobId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            var job = _exports.GetJob(jobId);
            if (job is { Status: ExportJobStatus.Completed or ExportJobStatus.Failed })
            {
                return;
            }

            await Task.Delay(20);
        }
    }
}
