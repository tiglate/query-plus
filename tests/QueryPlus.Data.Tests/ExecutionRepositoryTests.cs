using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using QueryPlus.Data.Context;
using QueryPlus.Data.Repositories;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Data.Tests;

public class ExecutionRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ExecutionRepository _sut;
    private readonly int _procId;

    public ExecutionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);

        var cat = new Category { Description = "General" };
        _db.Categories.Add(cat);
        _db.SaveChanges();

        var proc = new Procedure
        {
            IdCategory = cat.IdCategory,
            Caption = "Test SP",
            DatabaseName = "DB",
            ProcedureName = "sp_test",
            RoleEntitlement = ""
        };
        _db.Procedures.Add(proc);
        _db.SaveChanges();
        _procId = proc.IdProcedure;

        _sut = new ExecutionRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task AddAsync_And_GetByProcedureAsync_ReturnsLogsOrdered()
    {
        var log1 = new ExecutionLog
        {
            IdProcedure = _procId,
            Username = "alice",
            ExecutionStart = DateTime.UtcNow.AddMinutes(-10),
            Success = true,
            RowCount = 100
        };
        var log2 = new ExecutionLog
        {
            IdProcedure = _procId,
            Username = "bob",
            ExecutionStart = DateTime.UtcNow,
            Success = false,
            ErrorMessage = "Timeout"
        };

        await _sut.AddAsync(log1);
        await _sut.AddAsync(log2);
        await _db.SaveChangesAsync();

        var logs = await _sut.GetByProcedureAsync(_procId, take: 10);

        logs.Should().HaveCount(2);
        logs.First().Username.Should().Be("bob");
    }

    [Fact]
    public async Task SearchAsync_FiltersByUsernameAndSuccess()
    {
        await _sut.AddAsync(new ExecutionLog { IdProcedure = _procId, Username = "john", Success = true, ExecutionStart = DateTime.UtcNow });
        await _sut.AddAsync(new ExecutionLog { IdProcedure = _procId, Username = "john", Success = false, ExecutionStart = DateTime.UtcNow });
        await _sut.AddAsync(new ExecutionLog { IdProcedure = _procId, Username = "mary", Success = true, ExecutionStart = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var criteria = new ExecutionLogSearchCriteria { Username = "john", Success = true };
        var (items, total) = await _sut.SearchAsync(criteria, page: 1, pageSize: 10);

        total.Should().Be(1);
        items.Should().ContainSingle(l => l.Username == "john" && l.Success);
    }
}
