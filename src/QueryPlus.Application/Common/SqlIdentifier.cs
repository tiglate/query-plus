using System.Text.RegularExpressions;

namespace QueryPlus.Application.Common;

/// <summary>
/// Validates and quotes SQL Server identifiers to prevent injection via database/procedure names.
/// </summary>
public static partial class SqlIdentifier
{
    /// <summary>
    /// Single segment: letters, digits, underscore, optionally starting with letter/underscore.
    /// </summary>
    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SegmentRegex();

    public static bool IsValidSegment(string? value)
        => !string.IsNullOrWhiteSpace(value) && SegmentRegex().IsMatch(value);

    /// <summary>
    /// Procedure name may be "name" or "schema.name".
    /// </summary>
    public static bool IsValidProcedureName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length is 1 or 2 && parts.All(IsValidSegment);
    }

    public static string Quote(string segment)
    {
        if (!IsValidSegment(segment))
        {
            throw new ArgumentException($"Invalid SQL identifier: '{segment}'.", nameof(segment));
        }

        return $"[{segment.Replace("]", "]]", StringComparison.Ordinal)}]";
    }

    /// <summary>
    /// Builds [database].[schema].[procedure] for ADO.NET CommandType.StoredProcedure / three-part name.
    /// </summary>
    public static string BuildThreePartName(string databaseName, string procedureName)
    {
        if (!IsValidSegment(databaseName))
        {
            throw new ArgumentException($"Invalid database name: '{databaseName}'.", nameof(databaseName));
        }

        if (!IsValidProcedureName(procedureName))
        {
            throw new ArgumentException($"Invalid procedure name: '{procedureName}'.", nameof(procedureName));
        }

        var parts = procedureName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var schema = parts.Length == 2 ? parts[0] : "dbo";
        var name = parts.Length == 2 ? parts[1] : parts[0];

        return $"{Quote(databaseName)}.{Quote(schema)}.{Quote(name)}";
    }

    public static string NormalizeParameterName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalized = name.StartsWith('@') ? name : $"@{name}";
        var bare = normalized.TrimStart('@');
        if (!IsValidSegment(bare))
        {
            throw new ArgumentException(
                $"Invalid parameter name '{name}'. Only letters, digits, and underscore are allowed.",
                nameof(name));
        }

        return normalized;
    }
}
