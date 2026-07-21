using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.Mapping;
using QueryPlus.Application.Services;
using QueryPlus.Application.Validation;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Application.Tests;

public class ExecutionServiceTests
{
    private readonly IProcedureRepository _procedures = Substitute.For<IProcedureRepository>();
    private readonly IExecutionRepository _executions = Substitute.For<IExecutionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IStoredProcedureExecutor _executor = Substitute.For<IStoredProcedureExecutor>();
    private readonly ICurrentUserContext _user = Substitute.For<ICurrentUserContext>();
    private readonly ExecutionService _sut;

    public ExecutionServiceTests()
    {
        var mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<QueryPlusMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        _sut = new ExecutionService(
            _procedures,
            _executions,
            _unitOfWork,
            _executor,
            _user,
            mapper,
            new ExecuteProcedureRequestValidator(),
            NullLogger<ExecutionService>.Instance);
    }

    private static ExecutionLog MakeLog(int id, string username, bool success = true) => new()
    {
        IdExecutionLog = id,
        IdProcedure = 1,
        Username = username,
        ExecutionStart = DateTime.UtcNow,
        Success = success,
        Procedure = new Procedure
        {
            IdProcedure = 1,
            IdCategory = 1,
            Caption = "Invoices - List",
            DatabaseName = "QueryPlus",
            ProcedureName = "dbo.Sp_Invoices_List",
            RoleEntitlement = "user"
        }
    };

    [Fact]
    public async Task SearchAsync_ReturnsPagedResult_WithProcedureCaption()
    {
        var entities = new List<ExecutionLog> { MakeLog(1, "demo"), MakeLog(2, "demo") };
        _executions.SearchAsync(Arg.Any<ExecutionLogSearchCriteria>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns((entities, 2));

        var result = await _sut.SearchAsync(new ExecutionLogFilterDto { Username = "demo", Page = 1, PageSize = 20 });

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].ProcedureCaption.Should().Be("Invoices - List");
    }

    [Fact]
    public async Task SearchAsync_ClampsPage_WhenPastEnd()
    {
        _executions.SearchAsync(Arg.Any<ExecutionLogSearchCriteria>(), 5, 10, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<ExecutionLog>(), 12));
        _executions.SearchAsync(Arg.Any<ExecutionLogSearchCriteria>(), 2, 10, Arg.Any<CancellationToken>())
            .Returns((new List<ExecutionLog> { MakeLog(3, "demo") }, 12));

        var result = await _sut.SearchAsync(new ExecutionLogFilterDto { Page = 5, PageSize = 10 });

        result.Page.Should().Be(2);
        result.TotalCount.Should().Be(12);
        result.Items.Should().ContainSingle(i => i.Id == 3);
    }

    [Fact]
    public async Task SearchAsync_ConvertsLocalDateRange_ToUtcBounds()
    {
        ExecutionLogSearchCriteria? captured = null;
        _executions.SearchAsync(Arg.Do<ExecutionLogSearchCriteria>(c => captured = c), 1, 20, Arg.Any<CancellationToken>())
            .Returns(([], 0));

        var from = new DateTime(2026, 7, 1);
        var to = new DateTime(2026, 7, 5);
        await _sut.SearchAsync(new ExecutionLogFilterDto { StartFrom = from, StartTo = to, Page = 1, PageSize = 20 });

        captured.Should().NotBeNull();
        captured!.StartFrom.Should().Be(DateTime.SpecifyKind(from, DateTimeKind.Local).ToUniversalTime());
        // Upper bound is exclusive and covers the whole "to" calendar day.
        captured.StartTo.Should().Be(DateTime.SpecifyKind(to.AddDays(1), DateTimeKind.Local).ToUniversalTime());
    }
}
