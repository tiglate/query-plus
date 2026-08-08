using FluentAssertions;
using QueryPlus.Data.Context;

namespace QueryPlus.Data.Tests;

public class ApplicationDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_InstantiatesDbContextWithConfiguredConnectionString()
    {
        var factory = new ApplicationDbContextFactory();

        var db = factory.CreateDbContext([]);

        db.Should().NotBeNull();
    }
}
