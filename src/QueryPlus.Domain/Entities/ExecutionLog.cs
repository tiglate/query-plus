namespace QueryPlus.Domain.Entities;

/// <summary>
/// tb_execution_log
/// </summary>
public class ExecutionLog
{
    public int IdExecutionLog { get; set; }
    public int IdProcedure { get; set; }
    public required string Username { get; set; }
    public string? IpAddress { get; set; }
    public DateTime ExecutionStart { get; set; }
    public DateTime? ExecutionEnd { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    /// <summary>JSON of parameter values used in the run.</summary>
    public string? ParameterValues { get; set; }
    public int? RowCount { get; set; }

    public Procedure Procedure { get; set; } = null!;
}
