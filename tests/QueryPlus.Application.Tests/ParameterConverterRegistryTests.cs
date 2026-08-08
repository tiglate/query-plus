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

    [Theory]
    [InlineData("123", 123)] // int branch
    [InlineData("9999999999", 9999999999L)] // overflows int, falls through to long
    [InlineData("1.5", 1.5)] // falls through to decimal
    public void Convert_NumericType_FallsThroughToWidestFittingType(string raw, object expected)
    {
        var converter = _registry.GetConverter(ParameterType.Numeric);
        var param = new ProcedureParameter { IdProcedureParameter = 1, Name = "Amount", Caption = "Amount", ParameterType = ParameterType.Numeric };

        var result = converter.Convert(raw, param);

        result.Should().Be(expected);
    }

    [Fact]
    public void Convert_NumericType_InvalidValue_ThrowsFormatException()
    {
        var converter = _registry.GetConverter(ParameterType.Numeric);
        var param = new ProcedureParameter { IdProcedureParameter = 1, Name = "Amount", Caption = "Amount", ParameterType = ParameterType.Numeric };

        Action act = () => converter.Convert("not-a-number", param);

        act.Should().Throw<FormatException>().WithMessage("*is not a valid number*");
    }

    [Fact]
    public void Convert_DateType_ParsesIsoDate()
    {
        var converter = _registry.GetConverter(ParameterType.Date);
        var param = new ProcedureParameter { IdProcedureParameter = 1, Name = "AsOf", Caption = "As Of", ParameterType = ParameterType.Date };

        var result = converter.Convert("2026-03-05", param);

        result.Should().Be(new DateTime(2026, 3, 5));
    }

    [Fact]
    public void Convert_DateType_InvalidValue_ThrowsFormatException()
    {
        var converter = _registry.GetConverter(ParameterType.Date);
        var param = new ProcedureParameter { IdProcedureParameter = 1, Name = "AsOf", Caption = "As Of", ParameterType = ParameterType.Date };

        Action act = () => converter.Convert("not-a-date", param);

        act.Should().Throw<FormatException>().WithMessage("*not a valid date*");
    }

    [Fact]
    public void Convert_TimeType_ParsesTimeOfDay()
    {
        var converter = _registry.GetConverter(ParameterType.Time);
        var param = new ProcedureParameter { IdProcedureParameter = 1, Name = "StartTime", Caption = "Start Time", ParameterType = ParameterType.Time };

        var result = converter.Convert("13:45", param);

        result.Should().Be(new TimeSpan(13, 45, 0));
    }

    [Fact]
    public void Convert_TimeType_InvalidValue_ThrowsFormatException()
    {
        var converter = _registry.GetConverter(ParameterType.Time);
        var param = new ProcedureParameter { IdProcedureParameter = 1, Name = "StartTime", Caption = "Start Time", ParameterType = ParameterType.Time };

        Action act = () => converter.Convert("not-a-time", param);

        act.Should().Throw<FormatException>().WithMessage("*not a valid time*");
    }

    [Fact]
    public void Convert_DateTimeType_ParsesIsoDateTime()
    {
        var converter = _registry.GetConverter(ParameterType.DateTime);
        var param = new ProcedureParameter { IdProcedureParameter = 1, Name = "CreatedAt", Caption = "Created At", ParameterType = ParameterType.DateTime };

        var result = converter.Convert("2026-03-05T13:45:00", param);

        result.Should().Be(new DateTime(2026, 3, 5, 13, 45, 0));
    }

    [Fact]
    public void Convert_DateTimeType_InvalidValue_ThrowsFormatException()
    {
        var converter = _registry.GetConverter(ParameterType.DateTime);
        var param = new ProcedureParameter { IdProcedureParameter = 1, Name = "CreatedAt", Caption = "Created At", ParameterType = ParameterType.DateTime };

        Action act = () => converter.Convert("not-a-datetime", param);

        act.Should().Throw<FormatException>().WithMessage("*not a valid date/time*");
    }
}
