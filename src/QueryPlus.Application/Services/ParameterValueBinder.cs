using System.Globalization;
using QueryPlus.Application.Common;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;
using AppValidationException = QueryPlus.Application.Common.ValidationException;

namespace QueryPlus.Application.Services;

/// <summary>
/// Coerces string form values into typed SQL parameter values according to metadata.
/// </summary>
public static class ParameterValueBinder
{
    public static IReadOnlyDictionary<string, object?> Bind(
        IEnumerable<ProcedureParameter> definitions,
        IDictionary<string, string?> rawValues)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var bound = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var rawLookup = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in rawValues)
        {
            rawLookup[SqlIdentifier.NormalizeParameterName(key)] = value;
        }

        foreach (var definition in definitions)
        {
            var name = SqlIdentifier.NormalizeParameterName(definition.Name);
            rawLookup.TryGetValue(name, out var raw);

            if (string.IsNullOrWhiteSpace(raw))
            {
                raw = definition.DefaultValue;
            }

            if (IsMissingRequired(definition, raw))
            {
                errors[name] = [$"Parameter '{definition.Caption}' is required."];
                continue;
            }

            try
            {
                bound[name] = ConvertValue(definition.ParameterType, raw, definition);
            }
            catch (FormatException ex)
            {
                errors[name] = [ex.Message];
            }
            catch (AppValidationException vex)
            {
                errors[name] = vex.Errors.SelectMany(e => e.Value).ToArray();
            }
        }

        if (errors.Count > 0)
        {
            throw new AppValidationException(errors);
        }

        return bound;
    }

    /// <summary>
    /// Returns captions of required parameters that are still empty (no raw value and no default).
    /// Used by the web layer for pre-execution checks without throwing.
    /// </summary>
    public static IReadOnlyList<string> GetMissingRequiredCaptions(
        IEnumerable<ProcedureParameter> definitions,
        IDictionary<string, string?> rawValues)
    {
        var rawLookup = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in rawValues)
        {
            rawLookup[SqlIdentifier.NormalizeParameterName(key)] = value;
        }

        var missing = new List<string>();
        foreach (var definition in definitions)
        {
            var name = SqlIdentifier.NormalizeParameterName(definition.Name);
            rawLookup.TryGetValue(name, out var raw);
            if (string.IsNullOrWhiteSpace(raw))
            {
                raw = definition.DefaultValue;
            }

            if (IsMissingRequired(definition, raw))
            {
                missing.Add(definition.Caption);
            }
        }

        return missing;
    }

    private static bool IsMissingRequired(ProcedureParameter definition, string? effectiveValue)
    {
        if (!definition.IsRequired)
        {
            return false;
        }

        // Booleans always have a value (unchecked => false).
        if (definition.ParameterType == ParameterType.Boolean)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(effectiveValue);
    }

    private static object? ConvertValue(
        ParameterType type,
        string? raw,
        ProcedureParameter definition)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            // SQL NULL for empty optional inputs
            return type == ParameterType.Boolean ? false : null;
        }

        var value = raw.Trim();

        return type switch
        {
            ParameterType.FreeText => value,
            ParameterType.Combo => ValidateCombo(value, definition),
            ParameterType.Numeric => ParseNumeric(value),
            ParameterType.Date => DateOnly.Parse(value, CultureInfo.InvariantCulture)
                .ToDateTime(TimeOnly.MinValue),
            ParameterType.Time => TimeOnly.Parse(value, CultureInfo.InvariantCulture).ToTimeSpan(),
            ParameterType.DateTime => DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            ParameterType.Boolean => ParseBoolean(value),
            _ => throw new FormatException($"Unsupported parameter type '{type}'.")
        };
    }

    private static string ValidateCombo(string value, ProcedureParameter definition)
    {
        var options = JsonHelpers.ParseStringArray(definition.ComboValues);
        if (options.Count > 0 &&
            !options.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            throw new FormatException($"Value '{value}' is not in the allowed combo options.");
        }

        return value;
    }

    private static object ParseNumeric(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
        {
            return i;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
        {
            return l;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
        {
            return d;
        }

        throw new FormatException($"'{value}' is not a valid number.");
    }

    private static bool ParseBoolean(string value)
    {
        if (bool.TryParse(value, out var b))
        {
            return b;
        }

        return value switch
        {
            "1" or "yes" or "sim" or "on" => true,
            "0" or "no" or "não" or "nao" or "off" => false,
            _ => throw new FormatException($"'{value}' is not a valid boolean.")
        };
    }
}
