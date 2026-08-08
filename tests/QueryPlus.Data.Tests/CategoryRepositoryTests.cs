using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using QueryPlus.Data.Context;
using QueryPlus.Data.Repositories;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Data.Tests;

public class CategoryRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly CategoryRepository _sut;

    public CategoryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _sut = new CategoryRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_ReturnsCategory()
    {
        var category = new Category { Description = "Finance Reports" };
        await _sut.AddAsync(category);
        await _db.SaveChangesAsync();

        var fetched = await _sut.GetByIdAsync(category.IdCategory);

        fetched.Should().NotBeNull();
        fetched!.Description.Should().Be("Finance Reports");
    }

    [Fact]
    public async Task SearchAsync_FiltersByDescription_AndPaginate()
    {
        await _sut.AddAsync(new Category { Description = "Sales Analytics" });
        await _sut.AddAsync(new Category { Description = "Sales Operations" });
        await _sut.AddAsync(new Category { Description = "HR Payroll" });
        await _db.SaveChangesAsync();

        var (items, count) = await _sut.SearchAsync("Sales", page: 1, pageSize: 10);

        count.Should().Be(2);
        items.Should().HaveCount(2);
        items.Select(i => i.Description).Should().Contain("Sales Analytics", "Sales Operations");
    }

    [Fact]
    public async Task ExistsByDescriptionAsync_RespectsExcludeId()
    {
        var cat1 = new Category { Description = "Executive" };
        await _sut.AddAsync(cat1);
        await _db.SaveChangesAsync();

        var existsSameIdExcluded = await _sut.ExistsByDescriptionAsync("Executive", excludeId: cat1.IdCategory);
        var existsNoExclude = await _sut.ExistsByDescriptionAsync("Executive");

        existsSameIdExcluded.Should().BeFalse();
        existsNoExclude.Should().BeTrue();
    }

    [Fact]
    public async Task HasProceduresAsync_ReturnsTrue_WhenProcedureBelongsToCategory()
    {
        var cat = new Category { Description = "Operations" };
        await _sut.AddAsync(cat);
        await _db.SaveChangesAsync();

        _db.Procedures.Add(new Procedure
        {
            IdCategory = cat.IdCategory,
            Caption = "Test Proc",
            DatabaseName = "DB",
            ProcedureName = "sp_test",
            RoleEntitlement = ""
        });
        await _db.SaveChangesAsync();

        var hasProcs = await _sut.HasProceduresAsync(cat.IdCategory);

        hasProcs.Should().BeTrue();
    }

    [Fact]
    public async Task Update_And_Remove_Category()
    {
        var cat = new Category { Description = "To Update" };
        await _sut.AddAsync(cat);
        await _db.SaveChangesAsync();

        cat.Description = "Updated";
        _sut.Update(cat);
        await _db.SaveChangesAsync();

        var updated = await _sut.GetByIdAsync(cat.IdCategory);
        updated!.Description.Should().Be("Updated");

        _sut.Remove(cat);
        await _db.SaveChangesAsync();

        var removed = await _sut.GetByIdAsync(cat.IdCategory);
        removed.Should().BeNull();
    }
}
