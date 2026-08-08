using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QueryPlus.Data.Context;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Integration.Tests;

/// <summary>
/// Complements tests/QueryPlus.Data.Tests/UnitOfWorkTests.cs (SQLite): that suite proves
/// commit/rollback mechanics in isolation, but explicitly does not cover whether the real
/// AuditSaveChangesInterceptor's audit rows roll back atomically with the principal row against
/// the real target engine - that's what this class asserts.
/// </summary>
[Trait("Category", "Integration")]
public sealed class UnitOfWorkTransactionTests(SqlServerContainerFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task SaveChangesAsync_CommitsThePrincipalRowAndItsAuditRowTogether()
    {
        await WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var category = new Category { Description = $"IT-{Guid.NewGuid():N}", CreatedAt = DateTime.UtcNow };
            db.Categories.Add(category);

            await unitOfWork.SaveChangesAsync();

            category.IdCategory.Should().BePositive();
            (await db.CategoryAudits.CountAsync(a => a.IdCategory == category.IdCategory)).Should().Be(1);
        });
    }

    [Fact]
    public async Task SaveChangesAsync_RollsBackBothThePrincipalRowAndItsAuditRow_WhenSaveFails()
    {
        var description = $"IT-{Guid.NewGuid():N}";

        // Seed one category with this description so the duplicate insert below trips the real
        // uq_category_description unique index instead of an artificial failure.
        await WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            db.Categories.Add(new Category { Description = description, CreatedAt = DateTime.UtcNow });
            await unitOfWork.SaveChangesAsync();
        });

        await WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            db.Categories.Add(new Category { Description = description, CreatedAt = DateTime.UtcNow });

            var act = async () => await unitOfWork.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateException>();
        });

        await WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            (await db.Categories.CountAsync(c => c.Description == description)).Should().Be(1);
            // The failed insert's audit row must not have committed either - proves the
            // transaction wraps both the principal insert AND the interceptor's audit insert.
            (await db.CategoryAudits.CountAsync(a => a.Description == description)).Should().Be(1);
        });
    }
}
