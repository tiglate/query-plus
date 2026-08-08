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
    public async Task<ProcedureMetadataSnapshot> FetchAsync(
        string connectionName,
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

        var connectionString = configuration.GetConnectionString(connectionName)
                               ?? throw new InvalidOperationException(
                                   $"Connection string '{connectionName}' is not configured.");

        var parts = procedureName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var schema = parts.Length == 2 ? parts[0] : "dbo";
        var name = parts.Length == 2 ? parts[1] : parts[0];

        await using var connection = new SqlConnection(connectionString);
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
                   SELECT p.name AS param_name,
                          t.name AS type_name,
                          p.has_default_value,
                          p.default_value,
                          p.is_nullable
                   FROM {SqlIdentifier.Quote(databaseName)}.sys.parameters p
                   INNER JOIN {SqlIdentifier.Quote(databaseName)}.sys.types t ON p.user_type_id = t.user_type_id
                   WHERE p.object_id = OBJECT_ID(@qualifiedName)
                     AND p.parameter_id > 0
                   ORDER BY p.parameter_id;
                   """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add(new SqlParameter("@qualifiedName", SqlDbType.NVarChar, 4000)
        {
            Value = $"{databaseName}.{schema}.{procedureName}"
        });
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var list = new List<SaveProcedureParameterDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var paramName = reader.GetString(0);
            var typeName = reader.GetString(1);
            var hasDefault = reader.GetBoolean(2);
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
                DefaultValue = hasDefault ? DecodeDefault(reader, typeName) : null,
                ComboValues = null
            });
        }

        return list;
    }

    /// <summary>
    /// Decodes the sql_variant default value into a JSON-safe string.
    /// Numeric defaults are stored as raw bytes (hex); string defaults include surrounding quotes.
    /// </summary>
    private static string? DecodeDefault(SqlDataReader reader, string typeName)
    {
        try
        {
            var raw = reader.GetSqlValue(3);
            if (raw is null || raw is DBNull)
            {
                return null;
            }

            return typeName.ToLowerInvariant() switch
            {
                "bit" => Convert.ToInt64(raw) != 0 ? "true" : "false",
                "tinyint" => Convert.ToString(Convert.ToInt32(raw), System.Globalization.CultureInfo.InvariantCulture),
                "smallint" => Convert.ToString(Convert.ToInt16(raw), System.Globalization.CultureInfo.InvariantCulture),
                "int" => Convert.ToString(Convert.ToInt32(raw), System.Globalization.CultureInfo.InvariantCulture),
                "bigint" => Convert.ToString(Convert.ToInt64(raw), System.Globalization.CultureInfo.InvariantCulture),
                "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => Convert.ToString(raw,
                    System.Globalization.CultureInfo.InvariantCulture),
                "varchar" or "nvarchar" or "char" or "nchar" or "text" or "ntext" => TrimQuotes(raw.ToString() ??
                    string.Empty),
                "datetime" or "datetime2" or "date" or "time" or "smalldatetime" or "datetimeoffset" =>
                    Convert.ToString(raw, System.Globalization.CultureInfo.InvariantCulture),
                _ => raw.ToString()
            };
        }
        catch
        {
            return null;
        }
    }

    private static string TrimQuotes(string value)
    {
        if (value.Length >= 2 && value[0] == '\'' && value[^1] == '\'')
        {
            return value[1..^1];
        }

        return value;
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
}
