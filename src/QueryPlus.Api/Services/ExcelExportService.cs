using System.Collections.Concurrent;
using System.Threading.Channels;
using ClosedXML.Excel;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Common;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Services;
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

    public Guid QueueExport(int procedureId, IDictionary<string, string?> parameterValues, string username)
    {
        var id = Guid.NewGuid();
        _jobs[id] = new()
        {
            Id = id, ProcedureId = procedureId,
            ParameterValues = new(parameterValues, StringComparer.OrdinalIgnoreCase), Status = ExportJobStatus.Queued,
            CreatedAt = DateTime.UtcNow, Username = username
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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
                    IdProcedure = procedure.IdProcedure, Username = job.Username ?? "export-worker",
                    ExecutionStart = DateTime.UtcNow,
                    ParameterValues = JsonHelpers.Serialize(bound.ToDictionary(x => x.Key, x => x.Value?.ToString())),
                    Success = false
                };
                await executionRepository.AddAsync(log, stoppingToken);
                await unitOfWork.SaveChangesAsync(stoppingToken);
                var result = await scope.ServiceProvider.GetRequiredService<IStoredProcedureExecutor>()
                    .ExecuteAsync(procedure.DatabaseName, procedure.ProcedureName, parameters, outputs, stoppingToken);
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
                    sheet.Cell(1, 1).InsertTable(result.Data, true);
                    sheet.Columns().AdjustToContents();
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
}
