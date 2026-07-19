using FluentAssertions;
using QueryPlus.Application.Common;
using QueryPlus.Application.Services;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.Tests;

/// <summary>
/// Security-focused tests for parameter binding: wildcards, SQL fragments,
/// typed coercion, and closed combo allow-lists.
/// </summary>
public class ParameterValueBinderSecurityTests
{
    private static ProcedureParameter FreeText(string name = "@Q", bool required = false) => new()
    {
        Caption = "Query",
        Name = name,
        ParameterType = ParameterType.FreeText,
        IsRequired = required
    };

    [Theory]
    [InlineData("%")]
    [InlineData("%%")]
    [InlineData("_")]
    [InlineData("%_%")]
    public void Bind_FreeText_RejectsWildcardOnlyValues(string payload)
    {
        var defs = new[] { FreeText() };
        var raw = new Dictionary<string, string?> { ["@Q"] = payload };

        var act = () => ParameterValueBinder.Bind(defs, raw);

        act.Should().Throw<ValidationException>();
    }

    [Theory]
    [InlineData("' OR 1=1--")]
    [InlineData("1; DROP TABLE tb_demo_customer")]
    [InlineData("x UNION SELECT password FROM users")]
    [InlineData("admin'--")]
    public void Bind_FreeText_RejectsSqlInjectionPayloads(string payload)
    {
        var defs = new[] { FreeText() };
        var raw = new Dictionary<string, string?> { ["@Q"] = payload };

        var act = () => ParameterValueBinder.Bind(defs, raw);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Bind_FreeText_EscapesPercentSoLikeCannotMatchAll()
    {
        var defs = new[] { FreeText() };
        var raw = new Dictionary<string, string?> { ["@Q"] = "Acme%" };

        var bound = ParameterValueBinder.Bind(defs, raw);

        bound["@Q"].Should().Be("Acme[%]");
        bound["@Q"].As<string>().Should().NotBe("%");
        bound["@Q"].As<string>().Should().NotContain("%%");
    }

    [Fact]
    public void Bind_FreeText_EscapesUnderscoreAndBracket()
    {
        var defs = new[] { FreeText() };
        var raw = new Dictionary<string, string?> { ["@Q"] = "a_b[c" };

        var bound = ParameterValueBinder.Bind(defs, raw);

        bound["@Q"].Should().Be("a[_]b[[]c");
    }

    [Fact]
    public void Bind_FreeText_RejectsNullByte()
    {
        var defs = new[] { FreeText() };
        var raw = new Dictionary<string, string?> { ["@Q"] = "ab\0c" };

        var act = () => ParameterValueBinder.Bind(defs, raw);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Bind_FreeText_RejectsOverlongValue()
    {
        var defs = new[] { FreeText() };
        var raw = new Dictionary<string, string?>
        {
            ["@Q"] = new string('x', ParameterSecurity.MaxFreeTextLength + 50)
        };

        var act = () => ParameterValueBinder.Bind(defs, raw);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Bind_Combo_RejectsValueOutsideAllowList_IncludingSqlPayload()
    {
        var defs = new[]
        {
            new ProcedureParameter
            {
                Caption = "Country",
                Name = "@Country",
                ParameterType = ParameterType.Combo,
                ComboValues = """["USA","UK","Brazil"]"""
            }
        };

        var raw = new Dictionary<string, string?>
        {
            ["@Country"] = "USA' OR '1'='1"
        };

        var act = () => ParameterValueBinder.Bind(defs, raw);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Bind_Combo_RejectsWildcard()
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

        var raw = new Dictionary<string, string?> { ["@Status"] = "%" };

        var act = () => ParameterValueBinder.Bind(defs, raw);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Bind_Combo_ReturnsCanonicalOption()
    {
        var defs = new[]
        {
            new ProcedureParameter
            {
                Caption = "Country",
                Name = "@Country",
                ParameterType = ParameterType.Combo,
                ComboValues = """["USA","UK"]"""
            }
        };

        var raw = new Dictionary<string, string?> { ["@Country"] = "usa" };

        var bound = ParameterValueBinder.Bind(defs, raw);

        bound["@Country"].Should().Be("USA");
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("1; DROP TABLE x")]
    [InlineData("1 OR 1=1")]
    [InlineData("%")]
    public void Bind_Numeric_RejectsNonNumericAndInjection(string payload)
    {
        var defs = new[]
        {
            new ProcedureParameter
            {
                Caption = "Amount",
                Name = "@Amount",
                ParameterType = ParameterType.Numeric
            }
        };

        var raw = new Dictionary<string, string?> { ["@Amount"] = payload };

        var act = () => ParameterValueBinder.Bind(defs, raw);

        act.Should().Throw<ValidationException>();
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2024-99-99")]
    [InlineData("'; DROP TABLE")]
    public void Bind_Date_RejectsInvalidAndInjection(string payload)
    {
        var defs = new[]
        {
            new ProcedureParameter
            {
                Caption = "Start",
                Name = "@Start",
                ParameterType = ParameterType.Date
            }
        };

        var raw = new Dictionary<string, string?> { ["@Start"] = payload };

        var act = () => ParameterValueBinder.Bind(defs, raw);

        act.Should().Throw<ValidationException>();
    }

    [Theory]
    [InlineData("not-a-time")]
    [InlineData("25:00")]
    [InlineData("'; OR 1=1")]
    public void Bind_Time_RejectsInvalidAndInjection(string payload)
    {
        var defs = new[]
        {
            new ProcedureParameter
            {
                Caption = "At",
                Name = "@At",
                ParameterType = ParameterType.Time
            }
        };

        var raw = new Dictionary<string, string?> { ["@At"] = payload };

        var act = () => ParameterValueBinder.Bind(defs, raw);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Bind_IgnoresExtraUnboundRawKeys_DoesNotPassThroughInjectionParams()
    {
        // Only catalog-defined parameters are bound; attacker-supplied extra keys are ignored.
        var defs = new[] { FreeText("@Name") };
        var raw = new Dictionary<string, string?>
        {
            ["@Name"] = "Alice",
            ["@Evil"] = "'; DROP TABLE tb_demo_customer;--"
        };

        var bound = ParameterValueBinder.Bind(defs, raw);

        bound.Should().ContainKey("@Name");
        bound.Should().NotContainKey("@Evil");
        bound["@Name"].Should().Be("Alice");
    }

    [Fact]
    public void Bind_DoesNotUseRawKeysAsSqlIdentifiers()
    {
        // Even malicious parameter *names* in the raw dictionary are ignored if not in catalog.
        var defs = new[]
        {
            new ProcedureParameter
            {
                Caption = "Code",
                Name = "@Code",
                ParameterType = ParameterType.FreeText
            }
        };

        var raw = new Dictionary<string, string?>
        {
            ["@Code"] = "C001",
            ["@x; DROP TABLE t--"] = "1"
        };

        var bound = ParameterValueBinder.Bind(defs, raw);

        bound.Keys.Should().Equal("@Code");
    }
}
