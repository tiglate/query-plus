using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using QueryPlus.Application.Interfaces;

namespace QueryPlus.Data.StoredProcedures;

/// <summary>
/// Executes stored procedures using Dapper / ADO.NET and returns a DataTable.
/// Use this for dynamic result sets; use EF Core for regular CRUD.
/// </summary>
public class DapperStoredProcedureExecutor : IStoredProcedureExecutor
{
    private readonly string _connectionString;

    public DapperStoredProcedureExecutor(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }

    public async Task<DataTable> ExecuteAsync(
        string procedureName,
        IDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(procedureName);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var dynamicParameters = new DynamicParameters();
        if (parameters is not null)
        {
            foreach (var (name, value) in parameters)
            {
                dynamicParameters.Add(name.StartsWith('@') ? name : $"@{name}", value);
            }
        }

        await using var reader = await connection.ExecuteReaderAsync(
            new CommandDefinition(
                procedureName,
                dynamicParameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var table = new DataTable();
        table.Load(reader);
        return table;
    }
}
