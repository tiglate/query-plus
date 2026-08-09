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

    public DemoDataSeederTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private static IConfiguration BuildConfig(bool? seedDemoDataOnStartup)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=QueryPlusTest;"
        };
        if (seedDemoDataOnStartup.HasValue)
        {
            values["Database:SeedDemoDataOnStartup"] = seedDemoDataOnStartup.Value.ToString();
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public async Task SeedAsync_LoadsDemoCatalogFile_And_PopulatesCategoriesAndProcedures()
    {
        var seeder = new DemoDataSeeder(_db, BuildConfig(seedDemoDataOnStartup: true), NullLogger<DemoDataSeeder>.Instance);

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

    [Fact]
    public async Task SeedAsync_SkipsDemoData_WhenSettingIsAbsent()
    {
        var seeder = new DemoDataSeeder(_db, BuildConfig(seedDemoDataOnStartup: null), NullLogger<DemoDataSeeder>.Instance);

        await seeder.SeedAsync();

        (await _db.Categories.ToListAsync()).Should().BeEmpty();
        (await _db.Procedures.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task SeedAsync_SkipsDemoData_WhenSettingIsExplicitlyFalse()
    {
        var seeder = new DemoDataSeeder(_db, BuildConfig(seedDemoDataOnStartup: false), NullLogger<DemoDataSeeder>.Instance);

        await seeder.SeedAsync();

        (await _db.Categories.ToListAsync()).Should().BeEmpty();
        (await _db.Procedures.ToListAsync()).Should().BeEmpty();
    }
}
