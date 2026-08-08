using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using QueryPlus.Application.Interfaces;

namespace QueryPlus.Integration.Tests;

/// <summary>
/// SqlProcedureMetadataSyncService queries sys.parameters/sys.types and calls
/// sys.sp_describe_first_result_set - real SQL Server catalog views/system procedures that only a
/// real engine can exercise, so this is integration-only per design (see DapperStoredProcedureExecutorTests).
/// </summary>
[Trait("Category", "Integration")]
public sealed class SqlProcedureMetadataSyncServiceTests(SqlServerContainerFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task FetchAsync_PagedDemoProcedure_ReturnsItsRealDeclaredParameters_ExcludingReservedPagingArgs()
    {
        await WithScopeAsync(async sp =>
        {
            var sync = sp.GetRequiredService<IProcedureMetadataSyncService>();

            var snapshot = await sync.FetchAsync("DefaultConnection", DatabaseName, "dbo.Sp_Demo_Numbers_Paged");

            snapshot.Parameters.Should().ContainSingle(p => p.Name == "@MaxNumber");
            var reserved = new HashSet<string> { "@PageNumber", "@PageSize", "@TotalRecords" };
            snapshot.Parameters.Should().NotContain(p => reserved.Contains(p.Name));
        });
    }
}
