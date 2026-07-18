using QueryPlus.Application.DTOs.Execution;

namespace QueryPlus.Application.Interfaces;

public interface IExecutionService
{
    /// <summary>
    /// Validates access and parameters, executes the stored procedure securely,
    /// logs the run, and returns a DataTable-backed result for the grid.
    /// </summary>
    Task<ExecutionResultDto> ExecuteAsync(
        ExecuteProcedureRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExecutionLogDto>> GetRecentByProcedureAsync(
        int procedureId,
        int take = 50,
        CancellationToken cancellationToken = default);
}
