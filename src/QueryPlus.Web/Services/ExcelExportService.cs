using System.Collections.Concurrent;
using System.Threading.Channels;
using ClosedXML.Excel;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Common;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Services;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Web.Services;

public sealed class ExcelExportService : IExcelExportService
{
    private readonly ConcurrentDictionary<Guid, ExportJobState> _jobs = new();
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();
    private readonly string _exportDirectory;

    public ExcelExportService(IWebHostEnvironment env)
    {
        _exportDirectory = Path.Combine(env.ContentRootPath, "App_Data", "exports");
        Directory.CreateDirectory(_exportDirectory);
    }

    public ChannelReader<Guid> Reader => _queue.Reader;
    public string ExportDirectory => _exportDirectory;

    public Guid QueueExport(int procedureId, IDictionary<string, string?> parameterValues, string username)
    {
        var id = Guid.NewGuid();
        var job = new ExportJobState
        {
            Id = id,
            ProcedureId = procedureId,
            ParameterValues = new Dictionary<string, string?>(parameterValues, StringComparer.OrdinalIgnoreCase),
            Status = ExportJobStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            Username = username
        };

        _jobs[id] = job;
        _queue.Writer.TryWrite(id);
        return id;
    }

    public ExportJobDto? GetJob(Guid jobId)
        => _jobs.TryGetValue(jobId, out var job) ? job.ToDto() : null;

    public string? GetFilePath(Guid jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job) || job.Status != ExportJobStatus.Completed || job.FilePath is null)
        {
            return null;
        }

        return File.Exists(job.FilePath) ? job.FilePath : null;
    }

    public bool TryGetJobState(Guid id, out ExportJobState? state)
        => _jobs.TryGetValue(id, out state);

    public void UpdateJob(ExportJobState state) => _jobs[state.Id] = state;

    public sealed class ExportJobState
    {
        public Guid Id { get; init; }
        public int ProcedureId { get; init; }
        public Dictionary<string, string?> ParameterValues { get; init; } = new();
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
            Id = Id,
            Status = Status,
            FileName = FileName,
            ErrorMessage = ErrorMessage,
            RowCount = RowCount,
            CreatedAt = CreatedAt,
            CompletedAt = CompletedAt,
            Username = Username
        };
    }
}

/// <summary>
/// Processes queued Excel exports. Access was already authorized when the user clicked Export.
/// </summary>
public sealed class ExcelExportBackgroundService : BackgroundService
{
    private readonly ExcelExportService _exportService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExcelExportBackgroundService> _logger;

    public ExcelExportBackgroundService(
        ExcelExportService exportService,
        IServiceScopeFactory scopeFactory,
        ILogger<ExcelExportBackgroundService> logger)
    {
        _exportService = exportService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in _exportService.Reader.ReadAllAsync(stoppingToken))
        {
            if (!_exportService.TryGetJobState(jobId, out var job) || job is null)
            {
                continue;
            }

            job.Status = ExportJobStatus.Running;
            _exportService.UpdateJob(job);

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var procedures = scope.ServiceProvider.GetRequiredService<IProcedureRepository>();
                var executions = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var executor = scope.ServiceProvider.GetRequiredService<IStoredProcedureExecutor>();

                var procedure = await procedures.GetEnabledByIdWithDetailsAsync(job.ProcedureId, stoppingToken)
                    ?? throw new InvalidOperationException($"Procedure {job.ProcedureId} not found or disabled.");

                var bound = ParameterValueBinder.Bind(procedure.Parameters, job.ParameterValues);

                var log = new Domain.Entities.ExecutionLog
                {
                    IdProcedure = procedure.IdProcedure,
                    Username = job.Username ?? "export-worker",
                    ExecutionStart = DateTime.UtcNow,
                    ParameterValues = JsonHelpers.Serialize(
                        bound.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString())),
                    Success = false
                };
                await executions.AddAsync(log, stoppingToken);
                await unitOfWork.SaveChangesAsync(stoppingToken);

                var data = await executor.ExecuteAsync(
                    procedure.DatabaseName,
                    procedure.ProcedureName,
                    bound,
                    stoppingToken);

                log.Success = true;
                log.RowCount = data.Rows.Count;
                log.ExecutionEnd = DateTime.UtcNow;
                await unitOfWork.SaveChangesAsync(stoppingToken);

                var fileName = $"export_{job.ProcedureId}_{job.Id:N}.xlsx";
                var path = Path.Combine(_exportService.ExportDirectory, fileName);

                await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook();
                    var sheet = workbook.Worksheets.Add("Results");
                    sheet.Cell(1, 1).InsertTable(data, createTable: true);
                    sheet.Columns().AdjustToContents();
                    workbook.SaveAs(path);
                }, stoppingToken);

                job.FileName = fileName;
                job.FilePath = path;
                job.RowCount = data.Rows.Count;
                job.Status = ExportJobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                _exportService.UpdateJob(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel export job {JobId} failed", jobId);
                job.Status = ExportJobStatus.Failed;
                job.ErrorMessage = ex.Message;
                job.CompletedAt = DateTime.UtcNow;
                _exportService.UpdateJob(job);
            }
        }
    }
}
