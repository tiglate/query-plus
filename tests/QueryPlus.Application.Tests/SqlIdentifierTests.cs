using FluentAssertions;
using QueryPlus.Application.Common;

namespace QueryPlus.Application.Tests;

public class SqlIdentifierTests
{
    [Theory]
    [InlineData("Sales", "usp_Report", "[Sales].[dbo].[usp_Report]")]
    [InlineData("Sales", "reporting.usp_Report", "[Sales].[reporting].[usp_Report]")]
    public void BuildThreePartName_QuotesIdentifiers(string db, string proc, string expected)
    {
        SqlIdentifier.BuildThreePartName(db, proc).Should().Be(expected);
    }

    [Theory]
    [InlineData("Sales; DROP TABLE")]
    [InlineData("dbo.usp;--")]
    [InlineData("")]
    public void BuildThreePartName_RejectsInjection(string bad)
    {
        var act = () => SqlIdentifier.BuildThreePartName("Sales", bad);
        act.Should().Throw<ArgumentException>();
    }
}
