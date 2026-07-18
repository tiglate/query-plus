using System.Data;

namespace QueryPlus.Application.Interfaces;

/// <summary>
/// Executes stored procedures dynamically and returns results as a DataTable.
/// Implemented with Dapper / ADO.NET (not EF Core).
/// </summary>
public interface IStoredProcedureExecutor
{
    /// <summary>
    /// Executes a stored procedure and materializes the result set into a DataTable.
    /// </summary>
    /// <param name="procedureName">Fully qualified stored procedure name.</param>
    /// <param name="parameters">Optional named parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<DataTable> ExecuteAsync(
        string procedureName,
        IDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default);
}
