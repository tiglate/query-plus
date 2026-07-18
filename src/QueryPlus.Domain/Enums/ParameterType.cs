namespace QueryPlus.Domain.Enums;

/// <summary>
/// Stored in tb_procedure_parameter.parameter_type (VARCHAR(50)).
/// Values match the product specification.
/// </summary>
public enum ParameterType
{
    FreeText = 0,
    Numeric = 1,
    Date = 2,
    Time = 3,
    DateTime = 4,
    Boolean = 5,
    Combo = 6
}
