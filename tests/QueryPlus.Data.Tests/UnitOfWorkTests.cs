using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueryPlus.Data.Context;
using QueryPlus.Data.Repositories;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Data.Tests;

/// <summary>
/// EF Core's InMemory provider (used by the other Data.Tests) does not support transactions or
/// execution strategies, so UnitOfWork - which wraps SaveChangesAsync in an explicit transaction -
/// needs a real relational provider. SQLite's in-memory mode is used instead; the connection must
/// stay open for the test's lifetime or the in-memory database is dropped.
///
/// This does not cover AuditSaveChangesInterceptor committing atomically with the transaction -
/// that needs the real interceptor wired against a real engine and belongs in the Testcontainers
/// integration project instead.
/// </summary>
public class UnitOfWorkTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly UnitOfWork _sut;

    public UnitOfWorkTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        CreateCategorySchema();

        _context = new ApplicationDbContext(BuildOptions());
        _sut = new UnitOfWork(_context);
    }

    public void Dispose()
    {
        try { _context.Dispose(); } catch (ObjectDisposedException) { }
        _connection.Dispose();
    }

    private DbContextOptions<ApplicationDbContext> BuildOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

    /// <summary>
    /// Hand-rolled schema for just tb_category, mirroring CategoryConfiguration. The full model
    /// includes SQL-Server-only column types (e.g. nvarchar(max)) that EF's own
    /// Database.EnsureCreated() cannot translate for SQLite, so this test creates only the one
    /// table it actually needs rather than the whole ApplicationDbContext model.
    /// </summary>
    private void CreateCategorySchema()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE tb_category (
                id_category INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                description TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NULL
            );
            CREATE UNIQUE INDEX uq_category_description ON tb_category(description);
            """;
        command.ExecuteNonQuery();
    }

    [Fact]
    public async Task SaveChangesAsync_CommitsChange_VisibleFromAFreshContext()
    {
        _context.Categories.Add(new Category { Description = "Finance", CreatedAt = DateTime.UtcNow });

        var affected = await _sut.SaveChangesAsync();

        affected.Should().Be(1);

        await using var verifyContext = new ApplicationDbContext(BuildOptions());
        var count = await verifyContext.Categories.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_RollsBackTransaction_WhenSaveFails()
    {
        // Two entities with the same Description in one SaveChanges call violates the
        // uq_category_description unique index, forcing a real commit failure.
        _context.Categories.Add(new Category { Description = "Duplicate", CreatedAt = DateTime.UtcNow });
        _context.Categories.Add(new Category { Description = "Duplicate", CreatedAt = DateTime.UtcNow });

        var act = async () => await _sut.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();

        await using var verifyContext = new ApplicationDbContext(BuildOptions());
        var count = await verifyContext.Categories.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task DisposeAsync_DisposesTheUnderlyingContext()
    {
        await _sut.DisposeAsync();

        var act = async () => await _context.Categories.CountAsync();

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }
}
