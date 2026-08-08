using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using QueryPlus.Data.Context;
using QueryPlus.Data.Seed;

namespace QueryPlus.Data.Tests;

public class DemoDataSeederTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public DemoDataSeederTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new ApplicationDbContext(options);

        var myConfig = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=QueryPlusTest;"
        };
        _config = new ConfigurationBuilder().AddInMemoryCollection(myConfig).Build();
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task SeedAsync_LoadsDemoCatalogFile_And_PopulatesCategoriesAndProcedures()
    {
        var seeder = new DemoDataSeeder(_db, _config, NullLogger<DemoDataSeeder>.Instance);

        // Run seeding step against InMemory database
        await seeder.SeedAsync();

        var categories = await _db.Categories.ToListAsync();
        categories.Should().NotBeEmpty();

        var procedures = await _db.Procedures.Include(p => p.Parameters).Include(p => p.Columns).ToListAsync();
        procedures.Should().NotBeEmpty();

        var proc = procedures.First();
        proc.Caption.Should().NotBeNullOrWhiteSpace();
        proc.ConnectionName.Should().Be("DefaultConnection");
        proc.DatabaseName.Should().Be("QueryPlusTest");
    }
}
