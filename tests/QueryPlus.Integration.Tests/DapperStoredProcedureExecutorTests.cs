using System.Data;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QueryPlus.Application.Abstractions;

namespace QueryPlus.Integration.Tests;

/// <summary>
/// DapperStoredProcedureExecutor constructs its SqlConnection internally (not injected), so it
/// cannot be unit-tested with a mocked connection - its real execution path (parameter binding,
/// OUTPUT parameter round-tripping, OFFSET/FETCH pagination) is only exercised here, against
/// dbo.Sp_Demo_Numbers_Paged (installed by DemoDataSeeder - see IntegrationTestBase).
/// </summary>
[Trait("Category", "Integration")]
public sealed class DapperStoredProcedureExecutorTests(SqlServerContainerFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task ExecuteAsync_PagedDemoProcedure_ReturnsExpectedShapeAndOutputTotal()
    {
        await WithScopeAsync(async sp =>
        {
            var executor = sp.GetRequiredService<IStoredProcedureExecutor>();

            var page1 = await executor.ExecuteAsync(
                "DefaultConnection",
                DatabaseName,
                "dbo.Sp_Demo_Numbers_Paged",
                new Dictionary<string, object?> { ["@MaxNumber"] = 50, ["@PageNumber"] = 1L, ["@PageSize"] = 10L },
                outputParameterNames: ["@TotalRecords"]);

            page1.Data.Columns.Cast<DataColumn>().Select(c => c.ColumnName)
                .Should().BeEquivalentTo(["Number", "Code", "Label"]);
            page1.Data.Rows.Count.Should().Be(10);
            // @TotalRecords is declared BIGINT OUTPUT - this only catches SQL/CLR type-mapping
            // bugs (e.g. silent narrowing to int) against a real engine, not a mock.
            page1.TotalRecords.Should().Be(50L);
        });
    }

    [Fact]
    public async Task ExecuteAsync_SecondPage_ReturnsNonOverlappingRows()
    {
        await WithScopeAsync(async sp =>
        {
            var executor = sp.GetRequiredService<IStoredProcedureExecutor>();

            var page1 = await executor.ExecuteAsync(
                "DefaultConnection",
                DatabaseName,
                "dbo.Sp_Demo_Numbers_Paged",
                new Dictionary<string, object?> { ["@MaxNumber"] = 50, ["@PageNumber"] = 1L, ["@PageSize"] = 10L },
                outputParameterNames: ["@TotalRecords"]);
            var page2 = await executor.ExecuteAsync(
                "DefaultConnection",
                DatabaseName,
                "dbo.Sp_Demo_Numbers_Paged",
                new Dictionary<string, object?> { ["@MaxNumber"] = 50, ["@PageNumber"] = 2L, ["@PageSize"] = 10L },
                outputParameterNames: ["@TotalRecords"]);

            var page1Numbers = page1.Data.Rows.Cast<DataRow>().Select(r => (int)r["Number"]).ToArray();
            var page2Numbers = page2.Data.Rows.Cast<DataRow>().Select(r => (int)r["Number"]).ToArray();

            page2.Data.Rows.Count.Should().Be(10);
            page2Numbers.Should().NotIntersectWith(page1Numbers);
            page1Numbers.Should().BeEquivalentTo(Enumerable.Range(1, 10));
            page2Numbers.Should().BeEquivalentTo(Enumerable.Range(11, 10));
        });
    }

    /// <summary>
    /// Proves connectionName actually selects a distinct physical target: a procedure catalogued
    /// against "Server2" must execute on SecondaryDatabaseName, not DatabaseName (the default
    /// connection's database) - guards against ExecuteAsync silently ignoring connectionName and
    /// falling back to whatever connection it happened to be constructed with.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithNonDefaultConnectionName_TargetsThatServersDatabase()
    {
        await WithScopeAsync(async sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var secondaryConnectionString = configuration.GetConnectionString("Server2");

            await using (var connection = new SqlConnection(secondaryConnectionString))
            {
                await connection.OpenAsync();
                await using var create = connection.CreateCommand();
                create.CommandText = "CREATE PROCEDURE dbo.Sp_MultiServer_Marker AS SELECT DB_NAME() AS CurrentDatabase;";
                await create.ExecuteNonQueryAsync();
            }

            var executor = sp.GetRequiredService<IStoredProcedureExecutor>();

            var result = await executor.ExecuteAsync(
                "Server2",
                SecondaryDatabaseName,
                "dbo.Sp_MultiServer_Marker",
                new Dictionary<string, object?>());

            result.Data.Rows.Count.Should().Be(1);
            result.Data.Rows[0]["CurrentDatabase"].Should().Be(SecondaryDatabaseName);
        });
    }
}
