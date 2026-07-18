using System.Data;

namespace QueryPlus.Application.Abstractions;

/// <summary>
/// Executes registered stored procedures via ADO.NET/Dapper and returns a DataTable.
/// Never accepts arbitrary SQL — only database + procedure identifiers.
/// </summary>
public interface IStoredProcedureExecutor
{
    /// <param name="databaseName">Target database (validated identifier).</param>
    /// <param name="procedureName">Procedure name, optionally schema-qualified (e.g. dbo.usp_X).</param>
    /// <param name="parameters">Named parameters (values already typed/coerced).</param>
    Task<DataTable> ExecuteAsync(
        string databaseName,
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);
}
