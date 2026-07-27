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
    public async Task SearchAsync_FiltersByCaptionAndDatabase()
    {
        await _sut.AddAsync(new Procedure
        {
            IdCategory = _catId,
            Caption = "Sales Report",
            DatabaseName = "SalesDB",
            ProcedureName = "sp_sales",
            RoleEntitlement = ""
        });
        await _sut.AddAsync(new Procedure
        {
            IdCategory = _catId,
            Caption = "Customer List",
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
            DatabaseName = "DB",
            ProcedureName = "sp_admin",
            Enabled = true,
            RoleEntitlement = "admin-role"
        });
        await _sut.AddAsync(new Procedure
        {
            IdCategory = _catId,
            Caption = "Public Proc",
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
}
