using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using QueryPlus.Data.Context;
using QueryPlus.Data.Repositories;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Data.Tests;

public sealed class ConfigurationAuditReaderTests
{
    [Fact]
    public async Task GetCategoryAuditDetailsAsync_ReturnsInsertAndLatestUpdateUsers()
    {
        await using var db = CreateContext();
        var insert = Revision(1, "creator", new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        var firstUpdate = Revision(2, "first-editor", new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc));
        var latestUpdate = Revision(3, "latest-editor", new DateTime(2026, 1, 3, 10, 0, 0, DateTimeKind.Utc));

        db.CategoryAudits.AddRange(
            CategoryAudit(7, insert, RevisionTypeCode.Insert),
            CategoryAudit(7, latestUpdate, RevisionTypeCode.Update),
            CategoryAudit(7, firstUpdate, RevisionTypeCode.Update));
        await db.SaveChangesAsync();

        var result = await new ConfigurationAuditReader(db).GetCategoryAuditDetailsAsync(7);

        result.CreatedBy.Should().Be("creator");
        result.UpdatedBy.Should().Be("latest-editor");
    }

    [Fact]
    public async Task GetProcedureAuditDetailsAsync_ReturnsNullUpdater_WhenNeverUpdated()
    {
        await using var db = CreateContext();
        var insert = Revision(1, "creator", new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        db.ProcedureAudits.Add(new ProcedureAud
        {
            IdProcedure = 9,
            IdRevision = insert.IdRevision,
            IdRevisionType = RevisionTypeCode.Insert,
            Revision = insert
        });
        await db.SaveChangesAsync();

        var result = await new ConfigurationAuditReader(db).GetProcedureAuditDetailsAsync(9);

        result.CreatedBy.Should().Be("creator");
        result.UpdatedBy.Should().BeNull();
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Revision Revision(int id, string username, DateTime timestamp) => new()
    {
        IdRevision = id,
        Username = username,
        RevisionTimestamp = timestamp
    };

    private static CategoryAud CategoryAudit(
        int categoryId,
        Revision revision,
        RevisionTypeCode type) => new()
    {
        IdCategory = categoryId,
        IdRevision = revision.IdRevision,
        IdRevisionType = type,
        Revision = revision
    };
}
