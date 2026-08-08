using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.Services.Converters;

public sealed class ParameterConverterRegistry : IParameterConverterRegistry
{
    private readonly Dictionary<ParameterType, IParameterValueConverter> _converters;

    public ParameterConverterRegistry(IEnumerable<IParameterValueConverter> converters)
    {
        _converters = converters.ToDictionary(c => c.TargetType);
    }

    public static ParameterConverterRegistry CreateDefault()
    {
        return new ParameterConverterRegistry(new IParameterValueConverter[]
        {
            new FreeTextValueConverter(),
            new ComboValueConverter(),
            new NumericValueConverter(),
            new DateValueConverter(),
            new TimeValueConverter(),
            new DateTimeValueConverter(),
            new BooleanValueConverter()
        });
    }

    public IParameterValueConverter GetConverter(ParameterType type)
    {
        if (_converters.TryGetValue(type, out var converter))
        {
            return converter;
        }

        throw new FormatException($"Unsupported parameter type '{type}'.");
    }
}
