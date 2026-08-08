using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Integration.Tests;

/// <summary>
/// Representative smoke tests for the EF repositories against a real SQL Server engine, to catch
/// InMemory-vs-SqlServer semantic drift (real FK/unique-index enforcement, real generated keys).
/// Not a full re-implementation of the InMemory-backed suites in QueryPlus.Data.Tests.
/// </summary>
[Trait("Category", "Integration")]
public sealed class RepositoryCrudTests(SqlServerContainerFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task CategoryRepository_AddSearchUpdateRemove_RoundTripsAgainstRealServer()
    {
        await WithScopeAsync(async sp =>
        {
            var categories = sp.GetRequiredService<ICategoryRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

            var category = new Category { Description = $"IT-{Guid.NewGuid():N}", CreatedAt = DateTime.UtcNow };
            await categories.AddAsync(category);
            await unitOfWork.SaveChangesAsync();
            category.IdCategory.Should().BePositive();

            var (items, total) = await categories.SearchAsync(category.Description, 1, 10);
            total.Should().Be(1);
            items.Should().ContainSingle(c => c.IdCategory == category.IdCategory);

            category.Description += "-updated";
            categories.Update(category);
            await unitOfWork.SaveChangesAsync();

            var reloaded = await categories.GetByIdAsync(category.IdCategory);
            reloaded!.Description.Should().EndWith("-updated");

            categories.Remove(reloaded);
            await unitOfWork.SaveChangesAsync();

            (await categories.GetByIdAsync(category.IdCategory)).Should().BeNull();
        });
    }

    [Fact]
    public async Task ProcedureRepository_ExistsByDatabaseAndName_RespectsExcludeId_AgainstRealServer()
    {
        await WithScopeAsync(async sp =>
        {
            var procedures = sp.GetRequiredService<IProcedureRepository>();
            var categories = sp.GetRequiredService<ICategoryRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

            var category = new Category { Description = $"IT-{Guid.NewGuid():N}", CreatedAt = DateTime.UtcNow };
            await categories.AddAsync(category);
            await unitOfWork.SaveChangesAsync();

            var procedure = new Procedure
            {
                IdCategory = category.IdCategory,
                Caption = "IT Test Proc",
                DatabaseName = DatabaseName,
                ProcedureName = "dbo.sp_it_test",
                RoleEntitlement = "",
                CreatedAt = DateTime.UtcNow,
            };
            await procedures.AddAsync(procedure);
            await unitOfWork.SaveChangesAsync();

            (await procedures.ExistsByDatabaseAndNameAsync(DatabaseName, "dbo.sp_it_test")).Should().BeTrue();
            (await procedures.ExistsByDatabaseAndNameAsync(DatabaseName, "dbo.sp_it_test", excludeId: procedure.IdProcedure))
                .Should().BeFalse();
        });
    }

    [Fact]
    public async Task ExecutionRepository_AddAndSearch_RoundTripsAgainstRealServer()
    {
        await WithScopeAsync(async sp =>
        {
            var procedures = sp.GetRequiredService<IProcedureRepository>();
            var categories = sp.GetRequiredService<ICategoryRepository>();
            var executions = sp.GetRequiredService<IExecutionRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

            var category = new Category { Description = $"IT-{Guid.NewGuid():N}", CreatedAt = DateTime.UtcNow };
            await categories.AddAsync(category);
            await unitOfWork.SaveChangesAsync();

            var procedure = new Procedure
            {
                IdCategory = category.IdCategory,
                Caption = "IT Exec Proc",
                DatabaseName = DatabaseName,
                ProcedureName = "dbo.sp_it_exec",
                RoleEntitlement = "",
                CreatedAt = DateTime.UtcNow,
            };
            await procedures.AddAsync(procedure);
            await unitOfWork.SaveChangesAsync();

            var username = $"it-user-{Guid.NewGuid():N}";
            var log = new ExecutionLog
            {
                IdProcedure = procedure.IdProcedure,
                Username = username,
                ExecutionStart = DateTime.UtcNow,
                Success = true,
                RowCount = 3,
            };
            await executions.AddAsync(log);
            await unitOfWork.SaveChangesAsync();

            var (items, total) = await executions.SearchAsync(
                new ExecutionLogSearchCriteria { Username = username }, 1, 10);
            total.Should().Be(1);
            items.Should().ContainSingle(l => l.IdExecutionLog == log.IdExecutionLog);
        });
    }
}
