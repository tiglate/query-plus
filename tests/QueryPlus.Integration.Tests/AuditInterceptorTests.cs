using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QueryPlus.Data.Context;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Integration.Tests;

/// <summary>
/// Proves AuditSaveChangesInterceptor's real behavior against a real relational engine with real
/// identity-generated keys: the SQLite-backed UnitOfWorkTests deliberately stops short of this,
/// since the interceptor's temporary-key correction (raw ExecuteSqlInterpolated UPDATE against
/// tb_category_aud etc.) is SQL-Server-specific and only meaningful here.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AuditInterceptorTests(SqlServerContainerFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task InsertThenUpdateACategory_WritesMatchingAuditRows_WithTheRealGeneratedId()
    {
        var categoryId = await WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var category = new Category { Description = $"IT-{Guid.NewGuid():N}", CreatedAt = DateTime.UtcNow };
            db.Categories.Add(category);

            await unitOfWork.SaveChangesAsync();

            return category.IdCategory;
        });
        categoryId.Should().BePositive();

        await WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var category = await db.Categories.SingleAsync(c => c.IdCategory == categoryId);
            category.Description = "Updated by audit test";

            await unitOfWork.SaveChangesAsync();
        });

        await WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var auditRows = await db.CategoryAudits
                .Where(a => a.IdCategory == categoryId)
                .OrderBy(a => a.IdRevision)
                .ToListAsync();

            // Both rows must carry the REAL generated id_category, proving the interceptor's
            // temporary-key correction (SavedChangesAsync -> ExecuteSqlInterpolated) actually ran -
            // a bug here would leave the insert audit row keyed to EF's negative temporary id.
            auditRows.Should().HaveCount(2);
            auditRows.Should().OnlyContain(a => a.IdCategory == categoryId);
            auditRows[0].IdRevisionType.Should().Be(RevisionTypeCode.Insert);
            auditRows[1].IdRevisionType.Should().Be(RevisionTypeCode.Update);
            auditRows[1].Description.Should().Be("Updated by audit test");
        });
    }
}
