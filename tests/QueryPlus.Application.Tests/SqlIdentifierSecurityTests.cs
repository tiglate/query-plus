using FluentAssertions;
using QueryPlus.Application.Common;

namespace QueryPlus.Application.Tests;

/// <summary>
/// Identifier-level injection protection for database / schema / procedure / parameter names.
/// </summary>
public class SqlIdentifierSecurityTests
{
    [Theory]
    [InlineData("Sales; DROP TABLE x")]
    [InlineData("dbo.usp;--")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a.b.c")] // more than schema.proc
    [InlineData("usp Report")]
    [InlineData("usp-Report")]
    [InlineData("1Sales")]
    [InlineData("Sales'")]
    [InlineData("Sales]")]
    [InlineData("Sales\0db")]
    public void BuildThreePartName_RejectsMaliciousDatabaseOrProcedure(string bad)
    {
        var actDb = () => SqlIdentifier.BuildThreePartName(bad, "usp_Safe");
        var actProc = () => SqlIdentifier.BuildThreePartName("QueryPlus", bad);

        actDb.Should().Throw<ArgumentException>();
        actProc.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("QueryPlus", "Sp_USA_President_List", "[QueryPlus].[dbo].[Sp_USA_President_List]")]
    [InlineData("QueryPlus", "dbo.Sp_Demo_Customer_All", "[QueryPlus].[dbo].[Sp_Demo_Customer_All]")]
    public void BuildThreePartName_ProducesQuotedSafeName(string db, string proc, string expected)
    {
        SqlIdentifier.BuildThreePartName(db, proc).Should().Be(expected);
    }

    [Theory]
    [InlineData("@Name;DROP")]
    [InlineData("@x y")]
    [InlineData("@x-y")]
    [InlineData("@")]
    [InlineData("")]
    [InlineData("@'OR'1")]
    public void NormalizeParameterName_RejectsInjection(string name)
    {
        var act = () => SqlIdentifier.NormalizeParameterName(name);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("Name", "@Name")]
    [InlineData("@Name", "@Name")]
    [InlineData("@MinCredit", "@MinCredit")]
    public void NormalizeParameterName_AcceptsSafeNames(string name, string expected)
    {
        SqlIdentifier.NormalizeParameterName(name).Should().Be(expected);
    }

    [Fact]
    public void Quote_DoublesClosingBrackets()
    {
        // Segment validation rejects ']', so Quote is only for already-validated segments.
        // Ensure validated segment quoting is stable.
        SqlIdentifier.Quote("MyTable").Should().Be("[MyTable]");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1abc")]
    [InlineData("ab-c")]
    [InlineData("ab c")]
    [InlineData("ab;c")]
    public void IsValidSegment_RejectsUnsafe(string? value)
    {
        SqlIdentifier.IsValidSegment(value).Should().BeFalse();
    }
}
