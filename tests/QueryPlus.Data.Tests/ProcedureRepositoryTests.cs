using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using QueryPlus.Data.Context;
using QueryPlus.Data.Repositories;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Data.Tests;

public class ProcedureRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ProcedureRepository _sut;
    private readonly int _catId;

    public ProcedureRepositoryTests()
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
    public async Task AddAsync_And_GetByIdWithDetailsAsync_IncludesParametersAndColumns()
    {
        var proc = new Procedure
        {
            IdCategory = _catId,
            Caption = "Get Monthly Invoices",
            ConnectionName = "DefaultConnection",
            DatabaseName = "FinanceDB",
            ProcedureName = "sp_GetInvoices",
            RoleEntitlement = "finance-user",
            Parameters = new List<ProcedureParameter>
            {
                new() { Name = "@Month", Caption = "Month", ParameterType = Domain.Enums.ParameterType.Numeric }
            },
            Columns = new List<ProcedureColumn>
            {
                new() { TechnicalName = "InvoiceId", Caption = "Invoice #", Alignment = Domain.Enums.ColumnAlignment.Left }
            }
        };

        await _sut.AddAsync(proc);
        await _db.SaveChangesAsync();

        var fetched = await _sut.GetByIdWithDetailsAsync(proc.IdProcedure);

        fetched.Should().NotBeNull();
        fetched!.Caption.Should().Be("Get Monthly Invoices");
        fetched.Parameters.Should().HaveCount(1);
        fetched.Columns.Should().HaveCount(1);
        fetched.Parameters.First().Name.Should().Be("@Month");
    }

    [Fact]
    public async Task EnsureCategoryLoadedAsync_LoadsCategory_ForEntityAddedInThisContext()
    {
        // Mirrors ProcedureService.CreateAsync: entity built with only IdCategory set (no
        // Category navigation), added, saved - Category should still be null until loaded.
        var proc = new Procedure
        {
            IdCategory = _catId,
            Caption = "New Proc",
            ConnectionName = "DefaultConnection",
            DatabaseName = "DB",
            ProcedureName = "sp_new",
            RoleEntitlement = ""
        };
        await _sut.AddAsync(proc);
        await _db.SaveChangesAsync();

        await _sut.EnsureCategoryLoadedAsync(proc);

        proc.Category.Should().NotBeNull();
        proc.Category.Description.Should().Be("General");
    }

    [Fact]
    public async Task SearchAsync_FiltersByCaptionAndDatabase()
    {
        await _sut.AddAsync(new Procedure
        {
            IdCategory = _catId,
            Caption = "Sales Report",
            ConnectionName = "DefaultConnection",
            DatabaseName = "SalesDB",
            ProcedureName = "sp_sales",
            RoleEntitlement = ""
        });
        await _sut.AddAsync(new Procedure
        {
            IdCategory = _catId,
            Caption = "Customer List",
            ConnectionName = "DefaultConnection",
            DatabaseName = "CrmDB",
            ProcedureName = "sp_customers",
            RoleEntitlement = ""
        });
        await _db.SaveChangesAsync();

        var criteria = new ProcedureSearchCriteria { Caption = "Sales", DatabaseName = "SalesDB" };
        var (items, count) = await _sut.SearchAsync(criteria, page: 1, pageSize: 10);

        count.Should().Be(1);
        items.Should().ContainSingle(p => p.Caption == "Sales Report");
    }

    [Fact]
    public async Task GetAccessibleForExecutionAsync_FiltersByRole()
    {
        await _sut.AddAsync(new Procedure
        {
            IdCategory = _catId,
            Caption = "Admin Only Proc",
            ConnectionName = "DefaultConnection",
            DatabaseName = "DB",
            ProcedureName = "sp_admin",
            Enabled = true,
            RoleEntitlement = "admin-role"
        });
        await _sut.AddAsync(new Procedure
        {
            IdCategory = _catId,
            Caption = "Public Proc",
            ConnectionName = "DefaultConnection",
            DatabaseName = "DB",
            ProcedureName = "sp_public",
            Enabled = true,
            RoleEntitlement = ""
        });
        await _db.SaveChangesAsync();

        var userProcs = await _sut.GetAccessibleForExecutionAsync(["user-role"]);
        var adminProcs = await _sut.GetAccessibleForExecutionAsync(["user-role", "admin-role"]);

        userProcs.Select(p => p.Caption).Should().Contain("Public Proc").And.NotContain("Admin Only Proc");
        adminProcs.Select(p => p.Caption).Should().Contain("Public Proc", "Admin Only Proc");
    }

    [Fact]
    public async Task GetAccessibleForExecutionAsync_RoleAdmin_SeesEveryProcedure_RegardlessOfEntitlement()
    {
        await _sut.AddAsync(new Procedure
        {
            IdCategory = _catId,
            Caption = "Finance Only Proc",
            ConnectionName = "DefaultConnection",
            DatabaseName = "DB",
            ProcedureName = "sp_finance",
            Enabled = true,
            RoleEntitlement = "ROLE_FINANCE_TEAM"
        });
        await _db.SaveChangesAsync();

        // ROLE_ADMIN holds none of the procedure's own entitlement roles, but must still see it -
        // ROLE_ADMIN implies every permission, including running any catalogued procedure.
        var adminProcs = await _sut.GetAccessibleForExecutionAsync(["ROLE_ADMIN"]);

        adminProcs.Select(p => p.Caption).Should().Contain("Finance Only Proc");
    }
}
