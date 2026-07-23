using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueryPlus.Data.Context;
using QueryPlus.Data.Interceptors;
using QueryPlus.Data.Repositories;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Data.Tests;

/// <remarks>
/// Uses SQLite (a real relational provider that supports transactions and raw SQL) rather than
/// EF's InMemory provider, so these tests actually exercise AuditSaveChangesInterceptor's
/// post-save *_aud id correction the same way SQL Server does — InMemory can't run the raw
/// UPDATE statements that correction relies on.
/// </remarks>
public sealed class AuditSaveChangesInterceptorTests
{
    private sealed class TestAuditContext : IAuditContext
    {
        public string Username { get; set; } = "creator";
        public string? IpAddress => "127.0.0.1";
    }

    [Fact]
    public async Task CreatingCategory_CorrectsAuditRowIdToRealGeneratedKey()
    {
        var auditContext = new TestAuditContext();
        await using var db = await CreateContextAsync(auditContext);

        var category = new Category { Description = "Finance" };
        db.Categories.Add(category);
        await new UnitOfWork(db).SaveChangesAsync();

        category.IdCategory.Should().BePositive();

        var auditRow = await db.CategoryAudits.AsNoTracking().SingleAsync();
        auditRow.IdCategory.Should().Be(category.IdCategory);
        auditRow.IdRevisionType.Should().Be(RevisionTypeCode.Insert);
    }

    [Fact]
    public async Task CreatingProcedureWithParametersAndColumns_CorrectsAllAuditRowIds()
    {
        var auditContext = new TestAuditContext();
        await using var db = await CreateContextAsync(auditContext);

        var category = new Category { Description = "Sales" };
        db.Categories.Add(category);
        await new UnitOfWork(db).SaveChangesAsync();

        var procedure = new Procedure
        {
            IdCategory = category.IdCategory,
            Caption = "Sales Report",
            DatabaseName = "db",
            ProcedureName = "dbo.usp_Sales",
            RoleEntitlement = "user",
            Parameters =
            [
                new ProcedureParameter { Caption = "Year", Name = "@Year" }
            ],
            Columns =
            [
                new ProcedureColumn { TechnicalName = "Amount", Caption = "Amount" }
            ]
        };
        db.Procedures.Add(procedure);
        await new UnitOfWork(db).SaveChangesAsync();

        procedure.IdProcedure.Should().BePositive();
        procedure.Parameters.Single().IdProcedureParameter.Should().BePositive();
        procedure.Columns.Single().IdProcedureColumn.Should().BePositive();

        var procedureAudit = await db.ProcedureAudits.AsNoTracking().SingleAsync();
        procedureAudit.IdProcedure.Should().Be(procedure.IdProcedure);
        procedureAudit.IdCategory.Should().Be(category.IdCategory);

        var parameterAudit = await db.ProcedureParameterAudits.AsNoTracking().SingleAsync();
        parameterAudit.IdProcedureParameter.Should().Be(procedure.Parameters.Single().IdProcedureParameter);
        parameterAudit.IdProcedure.Should().Be(procedure.IdProcedure);

        var columnAudit = await db.ProcedureColumnAudits.AsNoTracking().SingleAsync();
        columnAudit.IdProcedureColumn.Should().Be(procedure.Columns.Single().IdProcedureColumn);
        columnAudit.IdProcedure.Should().Be(procedure.IdProcedure);
    }

    [Fact]
    public async Task ConfigurationAuditReader_ResolvesCreatedAndUpdatedBy_AfterRealCreateAndUpdateFlow()
    {
        var auditContext = new TestAuditContext { Username = "creator" };
        await using var db = await CreateContextAsync(auditContext);

        var category = new Category { Description = "Finance" };
        db.Categories.Add(category);
        await new UnitOfWork(db).SaveChangesAsync();

        var reader = new ConfigurationAuditReader(db);
        var afterCreate = await reader.GetCategoryAuditDetailsAsync(category.IdCategory);
        afterCreate.CreatedBy.Should().Be("creator");
        afterCreate.UpdatedBy.Should().BeNull();

        auditContext.Username = "editor";
        category.Description = "Finance Updated";
        db.Categories.Update(category);
        await new UnitOfWork(db).SaveChangesAsync();

        var afterUpdate = await reader.GetCategoryAuditDetailsAsync(category.IdCategory);
        afterUpdate.CreatedBy.Should().Be("creator");
        afterUpdate.UpdatedBy.Should().Be("editor");
    }

    private static async Task<ApplicationDbContext> CreateContextAsync(IAuditContext auditContext)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var interceptor = new AuditSaveChangesInterceptor(auditContext);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;

        var db = new DisposingConnectionContext(options, connection);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    /// <summary>
    /// Closes the backing SQLite connection when the context is disposed (":memory:" data
    /// disappears once the last connection closes), and strips SQL-Server-only column types
    /// (e.g. "nvarchar(max)") that SQLite's DDL parser rejects — production configuration is
    /// untouched; this only affects the model as seen by this test's SQLite connection.
    /// </summary>
    private sealed class DisposingConnectionContext(
        DbContextOptions<ApplicationDbContext> options,
        SqliteConnection connection) : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetProperties()))
            {
                if (property.GetColumnType() is string columnType
                    && columnType.Contains("(max)", StringComparison.OrdinalIgnoreCase))
                {
                    property.SetColumnType(null);
                }
            }
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
