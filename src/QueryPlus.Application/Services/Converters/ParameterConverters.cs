using System.Globalization;
using QueryPlus.Application.Common;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.Services.Converters;

public sealed class FreeTextValueConverter : IParameterValueConverter
{
    public ParameterType TargetType => ParameterType.FreeText;

    public object? Convert(string value, ProcedureParameter definition)
    {
        return ParameterSecurity.SanitizeAndValidateFreeText(value);
    }
}

public sealed class ComboValueConverter : IParameterValueConverter
{
    public ParameterType TargetType => ParameterType.Combo;

    public object? Convert(string value, ProcedureParameter definition)
    {
        var options = JsonHelpers.ParseStringArray(definition.ComboValues);
        if (options.Count == 0)
        {
            throw new FormatException("Combo parameter has no allowed options configured.");
        }

        if (!options.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            throw new FormatException($"Value '{value}' is not in the allowed combo options.");
        }

        return options.First(o => string.Equals(o, value, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class NumericValueConverter : IParameterValueConverter
{
    public ParameterType TargetType => ParameterType.Numeric;

    public object? Convert(string value, ProcedureParameter definition)
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
}

public sealed class DateValueConverter : IParameterValueConverter
{
    public ParameterType TargetType => ParameterType.Date;

    public object? Convert(string value, ProcedureParameter definition)
    {
        if (!DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            && !DateOnly.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
        {
            throw new FormatException($"'{value}' is not a valid date.");
        }

        return date.ToDateTime(TimeOnly.MinValue);
    }
}

public sealed class TimeValueConverter : IParameterValueConverter
{
    public ParameterType TargetType => ParameterType.Time;

    public object? Convert(string value, ProcedureParameter definition)
    {
        if (!TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            && !TimeOnly.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out time))
        {
            throw new FormatException($"'{value}' is not a valid time.");
        }

        return time.ToTimeSpan();
    }
}

public sealed class DateTimeValueConverter : IParameterValueConverter
{
    public ParameterType TargetType => ParameterType.DateTime;

    public object? Convert(string value, ProcedureParameter definition)
    {
        if (!DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces,
                out var dt)
            && !DateTime.TryParse(
                value,
                CultureInfo.CurrentCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out dt))
        {
            throw new FormatException($"'{value}' is not a valid date/time.");
        }

        return dt;
    }
}

public sealed class BooleanValueConverter : IParameterValueConverter
{
    public ParameterType TargetType => ParameterType.Boolean;

    public object? Convert(string value, ProcedureParameter definition)
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
