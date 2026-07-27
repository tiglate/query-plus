using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using QueryPlus.Data.Context;
using QueryPlus.Data.Repositories;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Data.Tests;

public class ProcedureRepositoryMoreTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ProcedureRepository _sut;
    private readonly int _catId;

    public ProcedureRepositoryMoreTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);

        var category = new Category { Description = "General" };
        _db.Categories.Add(category);
        _db.SaveChanges();
        _catId = category.IdCategory;

        _sut = new ProcedureRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllProcedures()
    {
        _db.Procedures.Add(new Procedure { IdCategory = _catId, Caption = "P1", DatabaseName = "DB", ProcedureName = "sp_1", RoleEntitlement = "" });
        _db.Procedures.Add(new Procedure { IdCategory = _catId, Caption = "P2", DatabaseName = "DB", ProcedureName = "sp_2", RoleEntitlement = "" });
        await _db.SaveChangesAsync();

        var list = await _sut.GetAllAsync();

        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExistsByCaptionAsync_ChecksExistenceAndExcludeId()
    {
        var p = new Procedure { IdCategory = _catId, Caption = "UniqueCaption", DatabaseName = "DB", ProcedureName = "sp_u", RoleEntitlement = "" };
        await _sut.AddAsync(p);
        await _db.SaveChangesAsync();

        var exists = await _sut.ExistsByCaptionAsync("UniqueCaption");
        var existsExcluded = await _sut.ExistsByCaptionAsync("UniqueCaption", excludeId: p.IdProcedure);

        exists.Should().BeTrue();
        existsExcluded.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByDatabaseAndNameAsync_ChecksExistenceAndExcludeId()
    {
        var p = new Procedure { IdCategory = _catId, Caption = "Cap", DatabaseName = "DB_A", ProcedureName = "sp_proc", RoleEntitlement = "" };
        await _sut.AddAsync(p);
        await _db.SaveChangesAsync();

        var exists = await _sut.ExistsByDatabaseAndNameAsync("DB_A", "sp_proc");
        var existsEx = await _sut.ExistsByDatabaseAndNameAsync("DB_A", "sp_proc", excludeId: p.IdProcedure);

        exists.Should().BeTrue();
        existsEx.Should().BeFalse();
    }

    [Fact]
    public async Task Remove_ParameterAndColumn_DeletesChildEntities()
    {
        var param = new ProcedureParameter { Name = "@P", Caption = "P", ParameterType = ParameterType.FreeText };
        var col = new ProcedureColumn { TechnicalName = "C", Caption = "C", Alignment = ColumnAlignment.Left };
        var proc = new Procedure
        {
            IdCategory = _catId,
            Caption = "WithChildren",
            DatabaseName = "DB",
            ProcedureName = "sp_child",
            RoleEntitlement = "",
            Parameters = new List<ProcedureParameter> { param },
            Columns = new List<ProcedureColumn> { col }
        };

        await _sut.AddAsync(proc);
        await _db.SaveChangesAsync();

        _sut.RemoveParameter(param);
        _sut.RemoveColumn(col);
        await _db.SaveChangesAsync();

        var reloaded = await _sut.GetByIdWithDetailsAsync(proc.IdProcedure);
        reloaded!.Parameters.Should().BeEmpty();
        reloaded.Columns.Should().BeEmpty();
    }

    [Fact]
    public async Task Update_And_Remove_Procedure()
    {
        var proc = new Procedure { IdCategory = _catId, Caption = "Before", DatabaseName = "DB", ProcedureName = "sp_b", RoleEntitlement = "" };
        await _sut.AddAsync(proc);
        await _db.SaveChangesAsync();

        proc.Caption = "After";
        _sut.Update(proc);
        await _db.SaveChangesAsync();

        var updated = await _sut.GetByIdAsync(proc.IdProcedure);
        updated!.Caption.Should().Be("After");

        _sut.Remove(proc);
        await _db.SaveChangesAsync();

        var removed = await _sut.GetByIdAsync(proc.IdProcedure);
        removed.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_FiltersByEnabledAndCategoryId()
    {
        var p1 = new Procedure { IdCategory = _catId, Caption = "Search Enabled", DatabaseName = "DB", ProcedureName = "sp_e", Enabled = true, RoleEntitlement = "" };
        var p2 = new Procedure { IdCategory = _catId, Caption = "Search Disabled", DatabaseName = "DB", ProcedureName = "sp_d", Enabled = false, RoleEntitlement = "" };
        await _sut.AddAsync(p1);
        await _sut.AddAsync(p2);
        await _db.SaveChangesAsync();

        var criteria1 = new ProcedureSearchCriteria { Caption = "Search", CategoryId = _catId, Enabled = true };
        var (enabledItems, enabledCount) = await _sut.SearchAsync(criteria1, page: 1, pageSize: 10);
        enabledCount.Should().Be(1);
        enabledItems.First().Caption.Should().Be("Search Enabled");

        var criteria2 = new ProcedureSearchCriteria { Caption = "Search", CategoryId = _catId, Enabled = false };
        var (disabledItems, disabledCount) = await _sut.SearchAsync(criteria2, page: 1, pageSize: 10);
        disabledCount.Should().Be(1);
        disabledItems.First().Caption.Should().Be("Search Disabled");
    }
}
