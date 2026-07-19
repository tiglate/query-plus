using FluentAssertions;
using QueryPlus.Application.Common;

namespace QueryPlus.Application.Tests;

public class ParameterSecurityTests
{
    // ─── LIKE wildcards ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("[")]
    [InlineData("%%")]
    [InlineData("%_%")]
    [InlineData("  %  ")]
    [InlineData("___")]
    public void SanitizeAndValidateFreeText_RejectsPureWildcardInput(string input)
    {
        var act = () => ParameterSecurity.SanitizeAndValidateFreeText(input);
        act.Should().Throw<FormatException>()
            .WithMessage("*wildcard*");
    }

    [Theory]
    [InlineData("Virginia%", "Virginia[%]")]
    [InlineData("%Virginia", "[%]Virginia")]
    [InlineData("John_Doe", "John[_]Doe")]
    [InlineData("100%", "100[%]")]
    [InlineData("a[b", "a[[]b")]
    [InlineData("test%_value", "test[%][_]value")]
    public void SanitizeAndValidateFreeText_EscapesEmbeddedLikeMetacharacters(
        string input,
        string expected)
    {
        ParameterSecurity.SanitizeAndValidateFreeText(input).Should().Be(expected);
    }

    [Fact]
    public void EscapeLikeMetacharacters_EscapesPercentUnderscoreAndBracket()
    {
        ParameterSecurity.EscapeLikeMetacharacters("%_[x]")
            .Should().Be("[%][_][[]x]");
    }

    // ─── Classic SQL injection fragments ──────────────────────────────────────

    [Theory]
    [InlineData("'; DROP TABLE tb_usa_president;--")]
    [InlineData("1; DELETE FROM tb_demo_customer")]
    [InlineData("x' OR '1'='1")]
    [InlineData("admin'--")]
    [InlineData("1 UNION SELECT * FROM tb_demo_customer")]
    [InlineData("'; EXEC xp_cmdshell('dir')")]
    [InlineData("foo; DROP TABLE users")]
    [InlineData("a OR 1=1")]
    [InlineData("test AND 1=1")]
    [InlineData("/*comment*/drop")]
    public void SanitizeAndValidateFreeText_RejectsSuspiciousSqlFragments(string input)
    {
        var act = () => ParameterSecurity.SanitizeAndValidateFreeText(input);
        act.Should().Throw<FormatException>()
            .WithMessage("*disallowed SQL*");
    }

    [Theory]
    [InlineData("O'Brien")] // apostrophe alone in a name is OK if no injection pattern
    [InlineData("Acme Corp")]
    [InlineData("São Paulo")]
    [InlineData("Jean-Luc")]
    [InlineData("Smith")]
    public void SanitizeAndValidateFreeText_AllowsLegitimateNames(string input)
    {
        // O'Brien contains ' but not the injection pattern (' followed by space/-- )
        // Note: our regex rejects '(\s|--) so "O'Brien" is fine.
        var act = () => ParameterSecurity.SanitizeAndValidateFreeText(input);
        act.Should().NotThrow();
    }

    // ─── Control characters / length ──────────────────────────────────────────

    [Fact]
    public void SanitizeAndValidateFreeText_RejectsNullCharacter()
    {
        var act = () => ParameterSecurity.SanitizeAndValidateFreeText("ab\0c");
        act.Should().Throw<FormatException>()
            .WithMessage("*null*");
    }

    [Fact]
    public void SanitizeAndValidateFreeText_StripsOtherControlCharacters()
    {
        // BEL (0x07) is stripped; remaining text is kept.
        ParameterSecurity.SanitizeAndValidateFreeText("ab\u0007c")
            .Should().Be("abc");
    }

    [Fact]
    public void SanitizeAndValidateFreeText_RejectsOverlongInput()
    {
        var longText = new string('a', ParameterSecurity.MaxFreeTextLength + 1);
        var act = () => ParameterSecurity.SanitizeAndValidateFreeText(longText);
        act.Should().Throw<FormatException>()
            .WithMessage("*at most*");
    }

    [Fact]
    public void SanitizeAndValidateFreeText_AllowsMaxLengthInput()
    {
        var text = new string('a', ParameterSecurity.MaxFreeTextLength);
        ParameterSecurity.SanitizeAndValidateFreeText(text).Should().Be(text);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("%", true)]
    [InlineData("_%", true)]
    [InlineData("  ", true)]
    [InlineData("a%", false)]
    [InlineData("ab", false)]
    public void IsOnlyLikeMetaOrWhitespace_DetectsPureMeta(string input, bool expected)
    {
        ParameterSecurity.IsOnlyLikeMetaOrWhitespace(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("@Valid_Name")]
    [InlineData("ValidName")]
    [InlineData("@p1")]
    public void EnsureSafeParameterName_AcceptsValidNames(string name)
    {
        var act = () => ParameterSecurity.EnsureSafeParameterName(name);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("@bad-name")]
    [InlineData("@drop table")]
    [InlineData("@x;y")]
    [InlineData("@")]
    [InlineData("")]
    [InlineData("@1start")] // must start with letter or underscore
    public void EnsureSafeParameterName_RejectsInvalidNames(string name)
    {
        var act = () => ParameterSecurity.EnsureSafeParameterName(name);
        act.Should().Throw<Exception>();
    }
}
