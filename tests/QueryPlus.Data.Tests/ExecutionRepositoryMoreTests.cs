using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using QueryPlus.Data.Context;
using QueryPlus.Data.Repositories;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Data.Tests;

public class ExecutionRepositoryMoreTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ExecutionRepository _sut;

    public ExecutionRepositoryMoreTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _sut = new ExecutionRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task SearchAsync_FiltersByUsername_Procedure_And_SuccessStatus()
    {
        var category = new Category { Description = "Test Cat" };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var proc = new Procedure { IdCategory = category.IdCategory, Caption = "P", DatabaseName = "DB", ProcedureName = "sp_p", RoleEntitlement = "" };
        _db.Procedures.Add(proc);
        await _db.SaveChangesAsync();

        var log1 = new ExecutionLog
        {
            IdProcedure = proc.IdProcedure,
            Username = "alice",
            ExecutionStart = DateTime.UtcNow.AddHours(-2),
            Success = true,
            ParameterValues = "{}"
        };

        var log2 = new ExecutionLog
        {
            IdProcedure = proc.IdProcedure,
            Username = "bob",
            ExecutionStart = DateTime.UtcNow.AddHours(-1),
            Success = false,
            ParameterValues = "{}"
        };

        await _sut.AddAsync(log1);
        await _sut.AddAsync(log2);
        await _db.SaveChangesAsync();

        var criteria1 = new Domain.Interfaces.ExecutionLogSearchCriteria { Username = "alice", ProcedureId = proc.IdProcedure, Success = true };
        var (aliceItems, aliceTotal) = await _sut.SearchAsync(criteria1, page: 1, pageSize: 10);
        aliceTotal.Should().Be(1);
        aliceItems.First().Username.Should().Be("alice");

        var criteria2 = new Domain.Interfaces.ExecutionLogSearchCriteria { Username = "bob", ProcedureId = proc.IdProcedure, Success = false };
        var (bobItems, bobTotal) = await _sut.SearchAsync(criteria2, page: 1, pageSize: 10);
        bobTotal.Should().Be(1);
        bobItems.First().Username.Should().Be("bob");
    }

    [Fact]
    public async Task GetByProcedureAsync_ReturnsLogsOrderedByDate()
    {
        var log1 = new ExecutionLog { IdProcedure = 5, Username = "user1", ExecutionStart = DateTime.UtcNow.AddMinutes(-10), Success = true, ParameterValues = "{}" };
        var log2 = new ExecutionLog { IdProcedure = 5, Username = "user2", ExecutionStart = DateTime.UtcNow.AddMinutes(-2), Success = true, ParameterValues = "{}" };

        await _sut.AddAsync(log1);
        await _sut.AddAsync(log2);
        await _db.SaveChangesAsync();

        var items = await _sut.GetByProcedureAsync(5, take: 10);
        items.Should().HaveCount(2);
        items.First().Username.Should().Be("user2");

        var userItems = await _sut.GetByUsernameAsync("user1", take: 10);
        userItems.Should().ContainSingle();
    }
}
