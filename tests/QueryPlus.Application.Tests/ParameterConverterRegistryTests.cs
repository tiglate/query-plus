using FluentAssertions;
using QueryPlus.Application.Services.Converters;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.Tests;

public class ParameterConverterRegistryTests
{
    private readonly IParameterConverterRegistry _registry = ParameterConverterRegistry.CreateDefault();

    [Theory]
    [InlineData(ParameterType.FreeText, " hello ", "hello")]
    [InlineData(ParameterType.Numeric, "123", 123)]
    [InlineData(ParameterType.Boolean, "true", true)]
    [InlineData(ParameterType.Boolean, "1", true)]
    [InlineData(ParameterType.Boolean, "0", false)]
    public void Convert_StandardTypes_ParsesValuesCorrectly(ParameterType type, string raw, object expected)
    {
        var converter = _registry.GetConverter(type);
        var param = new ProcedureParameter { IdProcedureParameter = 1, Name = "Test", Caption = "Test", ParameterType = type };

        var result = converter.Convert(raw, param);

        result.Should().Be(expected);
    }

    [Fact]
    public void Convert_ComboType_ReturnsCanonicalOption()
    {
        var converter = _registry.GetConverter(ParameterType.Combo);
        var param = new ProcedureParameter
        {
            IdProcedureParameter = 1,
            Name = "Status",
            Caption = "Status",
            ParameterType = ParameterType.Combo,
            ComboValues = "[\"ACTIVE\",\"INACTIVE\"]"
        };

        var result = converter.Convert("active", param);

        result.Should().Be("ACTIVE");
    }

    [Fact]
    public void Convert_ComboType_InvalidOption_ThrowsFormatException()
    {
        var converter = _registry.GetConverter(ParameterType.Combo);
        var param = new ProcedureParameter
        {
            IdProcedureParameter = 1,
            Name = "Status",
            Caption = "Status",
            ParameterType = ParameterType.Combo,
            ComboValues = "[\"ACTIVE\",\"INACTIVE\"]"
        };

        Action act = () => converter.Convert("UNKNOWN", param);

        act.Should().Throw<FormatException>().WithMessage("*not in the allowed combo options*");
    }
}
