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

    public async Task<StoredProcedureExecutionResult> ExecuteAsync(
        string databaseName,
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters,
        IReadOnlyCollection<string>? outputParameterNames = null,
        CancellationToken cancellationToken = default)
    {
        var qualifiedName = SqlIdentifier.BuildThreePartName(databaseName, procedureName);
        var outputs = NormalizeOutputNames(outputParameterNames);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var dynamicParameters = new DynamicParameters();
        foreach (var (name, value) in parameters)
        {
            ParameterSecurity.EnsureSafeParameterName(name);
            var paramName = SqlIdentifier.NormalizeParameterName(name);

            if (outputs.Contains(paramName))
            {
                // OUTPUT: still registered below with direction; skip value add here.
                continue;
            }

            if (value is string s && s.Contains('\0'))
            {
                throw new ArgumentException(
                    $"Parameter '{paramName}' contains an invalid null character.",
                    nameof(parameters));
            }

            dynamicParameters.Add(paramName, value ?? DBNull.Value);
        }

        foreach (var outputName in outputs)
        {
            ParameterSecurity.EnsureSafeParameterName(outputName);
            dynamicParameters.Add(
                outputName,
                dbType: DbType.Int64,
                direction: ParameterDirection.Output,
                value: 0L);
        }

        await using var reader = await connection.ExecuteReaderAsync(
            new CommandDefinition(
                commandText: qualifiedName,
                parameters: dynamicParameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: ProcedurePagination.CommandTimeoutSeconds,
                cancellationToken: cancellationToken));

        var table = new DataTable();
        table.Load(reader);

        long? totalRecords = null;
        if (outputs.Contains(ProcedurePagination.TotalRecordsName))
        {
            try
            {
                totalRecords = dynamicParameters.Get<long?>(ProcedurePagination.TotalRecordsName);
            }
            catch
            {
                // OUTPUT not returned by SP — leave null
                totalRecords = null;
            }
        }

        return new StoredProcedureExecutionResult
        {
            Data = table,
            TotalRecords = totalRecords
        };
    }

    private static HashSet<string> NormalizeOutputNames(IReadOnlyCollection<string>? names)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (names is null)
        {
            return set;
        }

        foreach (var n in names)
        {
            if (string.IsNullOrWhiteSpace(n))
            {
                continue;
            }

            set.Add(SqlIdentifier.NormalizeParameterName(n));
        }

        return set;
    }
}
