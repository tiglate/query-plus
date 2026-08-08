using System.Collections.Concurrent;
using System.Data;
using System.Threading.Channels;
using ClosedXML.Excel;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Common;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Services;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Api.Services;

public sealed class ExcelExportService : IExcelExportService
{
    private readonly ConcurrentDictionary<Guid, ExportJobState> _jobs = new();
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();

    public ExcelExportService(IWebHostEnvironment environment)
    {
        ExportDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "exports");
        Directory.CreateDirectory(ExportDirectory);
    }

    public ChannelReader<Guid> Reader => _queue.Reader;
    public string ExportDirectory { get; }

    public Guid QueueExport(int procedureId, IDictionary<string, string?> parameterValues, string username,
        IReadOnlyCollection<string> userRoles)
    {
        var id = Guid.NewGuid();
        _jobs[id] = new()
        {
            Id = id, ProcedureId = procedureId,
            ParameterValues = new(parameterValues, StringComparer.OrdinalIgnoreCase), Status = ExportJobStatus.Queued,
            CreatedAt = DateTime.UtcNow, Username = username, UserRoles = [..userRoles]
        };
        _queue.Writer.TryWrite(id);
        return id;
    }

    public ExportJobDto? GetJob(Guid jobId) => _jobs.TryGetValue(jobId, out var job) ? job.ToDto() : null;

    public string? GetFilePath(Guid jobId) =>
        _jobs.TryGetValue(jobId, out var job) && job.Status == ExportJobStatus.Completed && job.FilePath is not null &&
        File.Exists(job.FilePath)
            ? job.FilePath
            : null;

    public bool TryGetJobState(Guid id, out ExportJobState? state) => _jobs.TryGetValue(id, out state);
    public void UpdateJob(ExportJobState state) => _jobs[state.Id] = state;

    /// <summary>
    /// Evicts completed/failed jobs older than <paramref name="retention"/> (measured from
    /// CompletedAt, falling back to CreatedAt) and deletes their files. In-flight jobs (Queued,
    /// Running) are never evicted. Without this, both the in-memory job registry and
    /// App_Data/exports grow unbounded for the life of the process.
    /// </summary>
    public int EvictExpiredJobs(TimeSpan retention)
    {
        var cutoff = DateTime.UtcNow - retention;
        var evicted = 0;
        foreach (var (id, job) in _jobs)
        {
            if (job.Status is ExportJobStatus.Queued or ExportJobStatus.Running)
            {
                continue;
            }

            var referenceTime = job.CompletedAt ?? job.CreatedAt;
            if (referenceTime >= cutoff)
            {
                continue;
            }

            if (job.FilePath is not null && File.Exists(job.FilePath))
            {
                try
                {
                    File.Delete(job.FilePath);
                }
                catch (IOException)
                {
                    continue; // still in use (e.g. an in-flight download) - retry next sweep
                }
            }

            if (_jobs.TryRemove(id, out _))
            {
                evicted++;
            }
        }

        return evicted;
    }

    public sealed class ExportJobState
    {
        public Guid Id { get; init; }
        public int ProcedureId { get; init; }
        public Dictionary<string, string?> ParameterValues { get; init; } = [];
        public ExportJobStatus Status { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public string? ErrorMessage { get; set; }
        public int? RowCount { get; set; }
        public DateTime CreatedAt { get; init; }
        public DateTime? CompletedAt { get; set; }
        public string? Username { get; set; }
        public IReadOnlyCollection<string> UserRoles { get; init; } = [];

        public ExportJobDto ToDto() => new()
        {
            Id = Id, Status = Status, FileName = FileName, ErrorMessage = ErrorMessage, RowCount = RowCount,
            CreatedAt = CreatedAt, CompletedAt = CompletedAt, Username = Username
        };
    }
}

public sealed class ExcelExportBackgroundService(
    ExcelExportService exports,
    IServiceScopeFactory scopeFactory,
    ILogger<ExcelExportBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan JobRetention = TimeSpan.FromHours(1);
    private static readonly TimeSpan EvictionSweepInterval = TimeSpan.FromMinutes(10);

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(ProcessQueueAsync(stoppingToken), EvictExpiredJobsLoopAsync(stoppingToken));

    private async Task EvictExpiredJobsLoopAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(EvictionSweepInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var evicted = exports.EvictExpiredJobs(JobRetention);
                if (evicted > 0)
                {
                    logger.LogInformation("Evicted {Count} expired export job(s)", evicted);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        await foreach (var id in exports.Reader.ReadAllAsync(stoppingToken))
        {
            if (!exports.TryGetJobState(id, out var job) || job is null) continue;
            job.Status = ExportJobStatus.Running;
            exports.UpdateJob(job);
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var procedure =
                    await scope.ServiceProvider.GetRequiredService<IProcedureRepository>()
                        .GetEnabledByIdWithDetailsAsync(job.ProcedureId, stoppingToken) ??
                    throw new InvalidOperationException($"Procedure {job.ProcedureId} not found or disabled.");

                // Re-check entitlement at run time (not just at queue time): the job may sit in
                // the queue for a while, and the caller's role entitlement could have changed
                // since ExportsController.Queue captured it.
                if (!procedure.IsAccessibleTo(job.UserRoles))
                {
                    throw new ForbiddenOperationException(
                        $"User '{job.Username}' is no longer entitled to export procedure {job.ProcedureId}.");
                }

                var bound = ParameterValueBinder.Bind(
                    procedure.Parameters.Where(x => !ProcedurePagination.IsReservedParameterName(x.Name)),
                    job.ParameterValues);
                IReadOnlyDictionary<string, object?> parameters = bound;
                IReadOnlyCollection<string>? outputs = null;
                if (procedure.SupportsPagination)
                {
                    parameters = ProcedurePagination.WithPagingInputs(bound, ProcedurePagination.DefaultPageNumber,
                        ProcedurePagination.ExportPageSize);
                    outputs = [ProcedurePagination.TotalRecordsName];
                }

                var executionRepository = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var log = new Domain.Entities.ExecutionLog
                {
                    IdProcedure = procedure.IdProcedure, ConnectionName = procedure.ConnectionName,
                    Username = job.Username ?? "export-worker",
                    ExecutionStart = DateTime.UtcNow,
                    ParameterValues = JsonHelpers.Serialize(bound.ToDictionary(x => x.Key, x => x.Value?.ToString())),
                    Success = false
                };
                await executionRepository.AddAsync(log, stoppingToken);
                await unitOfWork.SaveChangesAsync(stoppingToken);
                var result = await scope.ServiceProvider.GetRequiredService<IStoredProcedureExecutor>()
                    .ExecuteAsync(procedure.ConnectionName, procedure.DatabaseName, procedure.ProcedureName, parameters, outputs, stoppingToken);
                log.Success = true;
                log.RowCount = procedure.SupportsPagination
                    ? (int)Math.Min(result.TotalRecords ?? result.Data.Rows.Count, int.MaxValue)
                    : result.Data.Rows.Count;
                log.ExecutionEnd = DateTime.UtcNow;
                await unitOfWork.SaveChangesAsync(stoppingToken);
                job.FileName = $"export_{job.ProcedureId}_{job.Id:N}.xlsx";
                job.FilePath = Path.Combine(exports.ExportDirectory, job.FileName);
                await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook();
                    var sheet = workbook.Worksheets.Add("Results");
                    WriteResultData(sheet, result.Data);
                    // Measuring every cell in every column is O(rows*cols) and dominates CPU on
                    // large exports; a bounded sample gives a reasonable width estimate without
                    // scanning the whole (potentially huge) result set.
                    var sampleEndRow = Math.Min(result.Data.Rows.Count + 1, AdjustToContentsSampleRows + 1);
                    sheet.Columns().AdjustToContents(1, sampleEndRow);
                    workbook.SaveAs(job.FilePath);
                }, stoppingToken);
                job.RowCount = result.Data.Rows.Count;
                job.Status = ExportJobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                exports.UpdateJob(job);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Excel export job {JobId} failed", id);
                job.Status = ExportJobStatus.Failed;
                job.ErrorMessage = ex.Message;
                job.CompletedAt = DateTime.UtcNow;
                exports.UpdateJob(job);
            }
        }
    }

    private const int AdjustToContentsSampleRows = 500;

    // Writes cells directly instead of IXLRangeBase.InsertTable(DataTable): InsertTable builds a
    // formatted Excel "structured table" (banding, filters, table-object metadata) on top of the
    // already-materialized DataTable, doubling memory/CPU overhead per cell for large exports
    // where none of that formatting is actually used.
    private static void WriteResultData(IXLWorksheet sheet, DataTable data)
    {
        for (var c = 0; c < data.Columns.Count; c++)
        {
            sheet.Cell(1, c + 1).Value = data.Columns[c].ColumnName;
        }

        for (var r = 0; r < data.Rows.Count; r++)
        {
            var row = data.Rows[r];
            for (var c = 0; c < data.Columns.Count; c++)
            {
                SetCellValue(sheet.Cell(r + 2, c + 1), row[c]);
            }
        }
    }

    private static void SetCellValue(IXLCell cell, object value)
    {
        switch (value)
        {
            case null or DBNull:
                break;
            case string s:
                cell.Value = s;
                break;
            case bool b:
                cell.Value = b;
                break;
            case DateTime dt:
                cell.Value = dt;
                break;
            case DateTimeOffset dto:
                cell.Value = dto.DateTime;
                break;
            case TimeSpan ts:
                cell.Value = ts;
                break;
            case byte[] bytes:
                cell.Value = Convert.ToBase64String(bytes);
                break;
            case double or float or decimal or byte or sbyte or short or ushort or int or uint or long or ulong:
                cell.Value = Convert.ToDouble(value);
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }
}
