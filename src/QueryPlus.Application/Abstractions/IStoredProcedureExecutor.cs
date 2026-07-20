using System.Data;

namespace QueryPlus.Application.Abstractions;

/// <summary>
/// Result of a catalog-registered stored procedure execution.
/// </summary>
public sealed class StoredProcedureExecutionResult
{
    public required DataTable Data { get; init; }

    /// <summary>
    /// Populated when the procedure uses server-side pagination (@TotalRecords OUTPUT).
    /// </summary>
    public long? TotalRecords { get; init; }
}

/// <summary>
/// Executes registered stored procedures via ADO.NET/Dapper and returns a DataTable.
/// Never accepts arbitrary SQL — only database + procedure identifiers.
/// </summary>
public interface IStoredProcedureExecutor
{
    /// <param name="databaseName">Target database (validated identifier).</param>
    /// <param name="procedureName">Procedure name, optionally schema-qualified (e.g. dbo.usp_X).</param>
    /// <param name="parameters">Named parameters (values already typed/coerced). OUTPUT params may be included with null placeholders.</param>
    /// <param name="outputParameterNames">Parameter names (e.g. @TotalRecords) registered as OUTPUT.</param>
    Task<StoredProcedureExecutionResult> ExecuteAsync(
        string databaseName,
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters,
        IReadOnlyCollection<string>? outputParameterNames = null,
        CancellationToken cancellationToken = default);
}
