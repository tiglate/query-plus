using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.Services.Converters;

public interface IParameterValueConverter
{
    ParameterType TargetType { get; }
    object? Convert(string value, ProcedureParameter definition);
}
