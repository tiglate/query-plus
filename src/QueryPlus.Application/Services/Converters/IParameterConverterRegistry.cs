using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.Services.Converters;

public interface IParameterConverterRegistry
{
    IParameterValueConverter GetConverter(ParameterType type);
}
