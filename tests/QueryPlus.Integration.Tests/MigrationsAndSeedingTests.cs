using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QueryPlus.Data.Context;
using QueryPlus.Data.Seed;

namespace QueryPlus.Integration.Tests;

[Trait("Category", "Integration")]
public sealed class MigrationsAndSeedingTests(SqlServerContainerFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Seeding_AppliesMigrations_InstallsDemoSqlObjects_AndSeedsCatalog()
    {
        // DemoDataSeeder.InstallSqlObjectsAsync swallows failures into a logged warning, so a
        // real assertion has to prove the object actually exists rather than trusting "no
        // exception was thrown" from IntegrationTestBase's InitializeAsync.
        await WithScopeAsync(async sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            await using var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT OBJECT_ID('dbo.Sp_Demo_Numbers_Paged', 'P');";
            var objectId = await cmd.ExecuteScalarAsync();

            objectId.Should().NotBeNull().And.NotBe(DBNull.Value);
        });

        await WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            (await db.Categories.AnyAsync()).Should().BeTrue();
            (await db.Procedures.AnyAsync()).Should().BeTrue();
        });
    }

    [Fact]
    public async Task Seeding_IsIdempotent_WhenRunASecondTime()
    {
        var (firstCategoryCount, firstProcedureCount) = await WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            return (await db.Categories.CountAsync(), await db.Procedures.CountAsync());
        });

        await WithScopeAsync(async sp =>
        {
            var seeder = sp.GetRequiredService<DemoDataSeeder>();
            await seeder.SeedAsync();
        });

        await WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            (await db.Categories.CountAsync()).Should().Be(firstCategoryCount);
            (await db.Procedures.CountAsync()).Should().Be(firstProcedureCount);
        });
    }
}

/// <summary>
/// Separate test class (own <see cref="IntegrationTestBase"/> instance/database) so the
/// pre-existing foreign table is in place before <see cref="IntegrationTestBase.InitializeAsync"/>
/// runs its own <see cref="DemoDataSeeder.SeedAsync"/> pass.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SeedingWithForeignTableTests(SqlServerContainerFixture fixture) : IntegrationTestBase(fixture)
{
    protected override async Task BeforeSeedAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        // Simulates a database shared with another, unrelated application.
        cmd.CommandText = "CREATE TABLE dbo.tb_unrelated_legacy_app (Id INT PRIMARY KEY);";
        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Seeding_SkipsDemoData_WhenDatabaseHasForeignTables()
    {
        await WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            (await db.Categories.AnyAsync()).Should().BeFalse();
            (await db.Procedures.AnyAsync()).Should().BeFalse();
        });

        await WithScopeAsync(async sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            await using var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT OBJECT_ID('dbo.Sp_Demo_Numbers_Paged', 'P');";
            var objectId = await cmd.ExecuteScalarAsync();

            (objectId is null or DBNull).Should().BeTrue();
        });
    }
}
