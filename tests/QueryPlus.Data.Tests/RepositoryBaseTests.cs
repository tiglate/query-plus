using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using QueryPlus.Data.Context;
using QueryPlus.Data.Repositories;
using QueryPlus.Domain.Entities;

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace QueryPlus.Data.Tests;

public class RepositoryBaseTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly Repository<Category> _sut;
    private readonly UnitOfWork _uow;

    public RepositoryBaseTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new ApplicationDbContext(options);
        _sut = new Repository<Category>(_db);
        _uow = new UnitOfWork(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task BaseRepository_Crud_SavesChanges()
    {
        var cat = new Category { Description = "Base Test" };
        await _sut.AddAsync(cat);
        await _db.SaveChangesAsync();

        var id = cat.IdCategory;
        id.Should().BePositive();

        var fetched = await _sut.GetByIdAsync(id);
        fetched.Should().NotBeNull();
        fetched!.Description.Should().Be("Base Test");

        var all = await _sut.GetAllAsync();
        all.Should().ContainSingle(c => c.IdCategory == id);

        var found = await _sut.FindAsync(c => c.Description.Contains("Base"));
        found.Should().ContainSingle();

        var count = await _sut.CountAsync(c => c.IdCategory == id);
        count.Should().Be(1);

        var exists = await _sut.ExistsAsync(id);
        exists.Should().BeTrue();

        cat.Description = "Base Updated";
        await _sut.UpdateAsync(cat);
        await _db.SaveChangesAsync();

        await _sut.DeleteAsync(cat);
        await _db.SaveChangesAsync();

        var existsAfterDelete = await _sut.ExistsAsync(id);
        existsAfterDelete.Should().BeFalse();
    }
}
