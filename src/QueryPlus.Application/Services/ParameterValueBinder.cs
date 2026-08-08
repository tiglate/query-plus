using System.Globalization;
using QueryPlus.Application.Common;
using QueryPlus.Application.Services.Converters;
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
        IDictionary<string, string?> rawValues,
        IParameterConverterRegistry? registry = null)
    {
        registry ??= ParameterConverterRegistry.CreateDefault();
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var bound = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var rawLookup = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in rawValues)
        {
            // Ignore attacker-supplied garbage keys; only catalog names are used.
            if (!TryNormalizeParameterName(key, out var normalizedKey))
            {
                continue;
            }

            rawLookup[normalizedKey] = value;
        }

        foreach (var definition in definitions)
        {
            // Pagination args are system-injected and must never be bound from user input.
            if (ProcedurePagination.IsReservedParameterName(definition.Name))
            {
                continue;
            }

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
                bound[name] = ConvertValue(definition.ParameterType, raw, definition, registry);
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
            if (!TryNormalizeParameterName(key, out var normalizedKey))
            {
                continue;
            }

            rawLookup[normalizedKey] = value;
        }

        var missing = new List<string>();
        foreach (var definition in definitions)
        {
            if (ProcedurePagination.IsReservedParameterName(definition.Name))
            {
                continue;
            }

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

    private static bool TryNormalizeParameterName(string name, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        try
        {
            normalized = SqlIdentifier.NormalizeParameterName(name);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static object? ConvertValue(
        ParameterType type,
        string? raw,
        ProcedureParameter definition,
        IParameterConverterRegistry registry)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            // SQL NULL for empty optional inputs
            return type == ParameterType.Boolean ? false : null;
        }

        var value = raw.Trim();
        var converter = registry.GetConverter(type);
        return converter.Convert(value, definition);
    }
}
