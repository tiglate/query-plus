using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using QueryPlus.Application.Common;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Interfaces;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Data.Metadata;

public sealed class SqlProcedureMetadataSyncService(IConfiguration configuration) : IProcedureMetadataSyncService
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
                                                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

    public async Task<ProcedureMetadataSnapshot> FetchAsync(
        string databaseName,
        string procedureName,
        CancellationToken cancellationToken = default)
    {
        if (!SqlIdentifier.IsValidSegment(databaseName))
        {
            throw new ArgumentException("Invalid database name.", nameof(databaseName));
        }

        if (!SqlIdentifier.IsValidProcedureName(procedureName))
        {
            throw new ArgumentException("Invalid procedure name.", nameof(procedureName));
        }

        var parts = procedureName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var schema = parts.Length == 2 ? parts[0] : "dbo";
        var name = parts.Length == 2 ? parts[1] : parts[0];

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var parameters = await LoadParametersAsync(connection, databaseName, schema, name, cancellationToken);
        var columns = await LoadColumnsAsync(connection, databaseName, schema, name, cancellationToken);

        return new ProcedureMetadataSnapshot
        {
            Parameters = parameters,
            Columns = columns
        };
    }

    private static async Task<IReadOnlyList<SaveProcedureParameterDto>> LoadParametersAsync(
        SqlConnection connection,
        string databaseName,
        string schema,
        string procedureName,
        CancellationToken cancellationToken)
    {
        // Qualified object name for OBJECT_ID in the target database context.
        var sql = $"""
            SELECT p.name AS param_name, t.name AS type_name
            FROM {SqlIdentifier.Quote(databaseName)}.sys.parameters p
            INNER JOIN {SqlIdentifier.Quote(databaseName)}.sys.types t ON p.user_type_id = t.user_type_id
            WHERE p.object_id = OBJECT_ID(N'{EscapeSqlLiteral(databaseName + "." + schema + "." + procedureName)}')
              AND p.parameter_id > 0
            ORDER BY p.parameter_id;
            """;

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var list = new List<SaveProcedureParameterDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var paramName = reader.GetString(0);
            var typeName = reader.GetString(1);
            // Never catalog reserved pagination parameters as user-facing fields.
            if (ProcedurePagination.IsReservedParameterName(paramName))
            {
                continue;
            }

            list.Add(new SaveProcedureParameterDto
            {
                Caption = paramName.TrimStart('@'),
                Name = paramName.StartsWith('@') ? paramName : "@" + paramName,
                ParameterType = MapSqlType(typeName),
                DefaultValue = null,
                ComboValues = null
            });
        }

        return list;
    }

    private static async Task<IReadOnlyList<SaveProcedureColumnDto>> LoadColumnsAsync(
        SqlConnection connection,
        string databaseName,
        string schema,
        string procedureName,
        CancellationToken cancellationToken)
    {
        // sp_describe_first_result_set works on the current connection database; switch context.
        await using (var useDb = new SqlCommand($"USE {SqlIdentifier.Quote(databaseName)};", connection))
        {
            await useDb.ExecuteNonQueryAsync(cancellationToken);
        }

        var tsql = $"EXEC {SqlIdentifier.Quote(schema)}.{SqlIdentifier.Quote(procedureName)}";
        await using var cmd = new SqlCommand("sys.sp_describe_first_result_set", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@tsql", tsql);
        cmd.Parameters.AddWithValue("@params", DBNull.Value);
        cmd.Parameters.AddWithValue("@browse_information_mode", 0);

        var list = new List<SaveProcedureColumnDto>();
        try
        {
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                // name column is index 2 in sp_describe_first_result_set
                var colName = reader["name"] as string;
                if (string.IsNullOrWhiteSpace(colName))
                {
                    continue;
                }

                list.Add(new SaveProcedureColumnDto
                {
                    TechnicalName = colName,
                    Caption = colName,
                    Alignment = ColumnAlignment.Left,
                    Visible = true
                });
            }
        }
        catch (SqlException)
        {
            // Procedure may require parameters; columns remain empty — admin can add manually.
        }

        return list;
    }

    private static ParameterType MapSqlType(string sqlType) => sqlType.ToLowerInvariant() switch
    {
        "bit" => ParameterType.Boolean,
        "int" or "bigint" or "smallint" or "tinyint" or "decimal" or "numeric"
            or "money" or "smallmoney" or "float" or "real" => ParameterType.Numeric,
        "date" => ParameterType.Date,
        "time" => ParameterType.Time,
        "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" => ParameterType.DateTime,
        _ => ParameterType.FreeText
    };

    private static string EscapeSqlLiteral(string value)
        => value.Replace("'", "''", StringComparison.Ordinal);
}
