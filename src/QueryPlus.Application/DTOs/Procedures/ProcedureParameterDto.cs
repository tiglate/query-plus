using QueryPlus.Application.Common;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.DTOs.Procedures;

public sealed class ProcedureParameterDto
{
    public int Id { get; init; }
    public required string Caption { get; init; }

    /// <summary>SQL parameter name, e.g. @StartDate.</summary>
    public required string Name { get; init; }

    public ParameterType ParameterType { get; init; }
    public string? DefaultValue { get; init; }

    /// <summary>JSON array string for Combo type.</summary>
    public string? ComboValues { get; init; }

    public bool IsRequired { get; init; }

    /// <summary>
    /// Parsed, trimmed entries from <see cref="ComboValues"/>. Returns an empty
    /// list when the JSON is null, blank, or malformed so callers never have to
    /// null-check on the wire.
    /// </summary>
    public IReadOnlyList<string> ComboOptions => JsonHelpers.ParseStringArray(ComboValues);
}
