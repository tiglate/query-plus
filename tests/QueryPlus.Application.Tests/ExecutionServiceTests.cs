using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.DTOs.Execution;
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
        _sut = new ExecutionService(
            _procedures,
            _executions,
            _unitOfWork,
            _executor,
            _user,
            new ExecuteProcedureRequestValidator(),
            new ExecutionParameterResolver(QueryPlus.Application.Services.Converters.ParameterConverterRegistry.CreateDefault()),
            new GridColumnBuilder(),
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
        _executions.SearchAsync(Arg.Do<ExecutionLogSearchCriteria>(c => captured = c), 1, 20,
                Arg.Any<CancellationToken>())
            .Returns(([], 0));

        var from = new DateTime(2026, 7, 1);
        var to = new DateTime(2026, 7, 5);
        await _sut.SearchAsync(new ExecutionLogFilterDto { StartFrom = from, StartTo = to, Page = 1, PageSize = 20 });

        captured.Should().NotBeNull();
        captured!.StartFrom.Should().Be(DateTime.SpecifyKind(from, DateTimeKind.Local).ToUniversalTime());
        // Upper bound is exclusive and covers the whole "to" calendar day.
        captured.StartTo.Should().Be(DateTime.SpecifyKind(to.AddDays(1), DateTimeKind.Local).ToUniversalTime());
    }

    [Fact]
    public async Task ExecuteAsync_RedactsSensitiveParameters_InExecutionLog()
    {
        var procedure = new Procedure
        {
            IdProcedure = 10,
            IdCategory = 1,
            Caption = "Auth Proc",
            DatabaseName = "DB",
            ProcedureName = "sp_auth",
            Enabled = true,
            RoleEntitlement = "",
            Parameters = new List<ProcedureParameter>
            {
                new() { IdProcedureParameter = 1, Name = "@Username", Caption = "User", ParameterType = Domain.Enums.ParameterType.FreeText, IsSensitive = false },
                new() { IdProcedureParameter = 2, Name = "@Password", Caption = "Pass", ParameterType = Domain.Enums.ParameterType.FreeText, IsSensitive = true }
            }
        };

        _user.IsAuthenticated.Returns(true);
        _user.Username.Returns("admin");
        _procedures.GetEnabledByIdWithDetailsAsync(10, Arg.Any<CancellationToken>()).Returns(procedure);

        ExecutionLog? savedLog = null;
        await _executions.AddAsync(Arg.Do<ExecutionLog>(l => savedLog = l), Arg.Any<CancellationToken>());

        _executor.ExecuteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<IReadOnlyCollection<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new StoredProcedureExecutionResult { Data = new System.Data.DataTable() });

        var request = new ExecuteProcedureRequest
        {
            ProcedureId = 10,
            ParameterValues = new Dictionary<string, string?>
            {
                ["@Username"] = "john_doe",
                ["@Password"] = "SuperSecret123!"
            }
        };

        await _sut.ExecuteAsync(request);

        savedLog.Should().NotBeNull();
        savedLog!.ParameterValues.Should().Contain("\"@Username\":\"john[_]doe\"");
        savedLog.ParameterValues.Should().Contain("\"@Password\":\"***\"");
        savedLog.ParameterValues.Should().NotContain("SuperSecret123!");
    }
}
