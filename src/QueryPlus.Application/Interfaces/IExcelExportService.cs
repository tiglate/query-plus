namespace QueryPlus.Application.Interfaces;

public interface IExcelExportService
{
    /// <summary>Queues a background export. Returns job id for polling.</summary>
    Guid QueueExport(int procedureId, IDictionary<string, string?> parameterValues, string username);

    ExportJobDto? GetJob(Guid jobId);

    /// <summary>Absolute path to the generated file when completed.</summary>
    string? GetFilePath(Guid jobId);
}

public sealed class ExportJobDto
{
    public Guid Id { get; init; }
    public ExportJobStatus Status { get; init; }
    public string? FileName { get; init; }
    public string? ErrorMessage { get; init; }
    public int? RowCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? Username { get; init; }
}

public enum ExportJobStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}
