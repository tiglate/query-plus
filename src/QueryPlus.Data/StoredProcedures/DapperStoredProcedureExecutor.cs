using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Common;

namespace QueryPlus.Data.StoredProcedures;

/// <summary>
/// Executes catalog-registered stored procedures via Dapper/ADO.NET.
/// Only validated identifiers and bound parameters are accepted — never free SQL.
/// </summary>
public sealed class DapperStoredProcedureExecutor : IStoredProcedureExecutor
{
    private readonly string _connectionString;

    public DapperStoredProcedureExecutor(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }

    public async Task<DataTable> ExecuteAsync(
        string databaseName,
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        // Defense in depth: re-validate identifiers even if caller already checked.
        var qualifiedName = SqlIdentifier.BuildThreePartName(databaseName, procedureName);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var dynamicParameters = new DynamicParameters();
        foreach (var (name, value) in parameters)
        {
            // Reject anything that is not a simple parameter name (blocks name injection).
            ParameterSecurity.EnsureSafeParameterName(name);
            var paramName = SqlIdentifier.NormalizeParameterName(name);

            // Never pass free-form SQL fragments as command text — values only.
            if (value is string s && s.Contains('\0'))
            {
                throw new ArgumentException(
                    $"Parameter '{paramName}' contains an invalid null character.",
                    nameof(parameters));
            }

            dynamicParameters.Add(paramName, value ?? DBNull.Value);
        }

        await using var reader = await connection.ExecuteReaderAsync(
            new CommandDefinition(
                commandText: qualifiedName,
                parameters: dynamicParameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120,
                cancellationToken: cancellationToken));

        var table = new DataTable();
        table.Load(reader);
        return table;
    }
}
