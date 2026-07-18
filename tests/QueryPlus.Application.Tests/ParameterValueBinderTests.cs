using FluentAssertions;
using QueryPlus.Application.Common;
using QueryPlus.Application.Services;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.Tests;

public class ParameterValueBinderTests
{
    [Fact]
    public void Bind_CoercesNumericAndDate()
    {
        var defs = new[]
        {
            new ProcedureParameter { Caption = "Qty", Name = "@Qty", ParameterType = ParameterType.Numeric },
            new ProcedureParameter { Caption = "Day", Name = "Day", ParameterType = ParameterType.Date }
        };

        var raw = new Dictionary<string, string?>
        {
            ["@Qty"] = "42",
            ["Day"] = "2026-07-18"
        };

        var bound = ParameterValueBinder.Bind(defs, raw);

        bound["@Qty"].Should().Be(42);
        bound["@Day"].Should().Be(new DateTime(2026, 7, 18));
    }

    [Fact]
    public void Bind_RejectsInvalidCombo()
    {
        var defs = new[]
        {
            new ProcedureParameter
            {
                Caption = "Status",
                Name = "@Status",
                ParameterType = ParameterType.Combo,
                ComboValues = """["Open","Closed"]"""
            }
        };

        var raw = new Dictionary<string, string?> { ["@Status"] = "Unknown" };

        var act = () => ParameterValueBinder.Bind(defs, raw);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Bind_RejectsMissingRequiredParameter()
    {
        var defs = new[]
        {
            new ProcedureParameter
            {
                Caption = "State",
                Name = "@State",
                ParameterType = ParameterType.Combo,
                IsRequired = true,
                ComboValues = """["Virginia","Ohio"]"""
            }
        };

        var raw = new Dictionary<string, string?>();

        var act = () => ParameterValueBinder.Bind(defs, raw);

        act.Should().Throw<ValidationException>()
            .Which.Errors.Should().ContainKey("@State");
    }

    [Fact]
    public void GetMissingRequiredCaptions_ReturnsEmpty_WhenDefaultExists()
    {
        var defs = new[]
        {
            new ProcedureParameter
            {
                Caption = "Min",
                Name = "@Min",
                ParameterType = ParameterType.Numeric,
                IsRequired = true,
                DefaultValue = "10"
            }
        };

        var missing = ParameterValueBinder.GetMissingRequiredCaptions(defs, new Dictionary<string, string?>());

        missing.Should().BeEmpty();
    }
}
