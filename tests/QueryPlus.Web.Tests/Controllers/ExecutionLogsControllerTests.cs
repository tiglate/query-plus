using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Interfaces;
using QueryPlus.Web.Controllers.Admin;
using QueryPlus.Web.ViewModels;

namespace QueryPlus.Web.Tests.Controllers;

public sealed class ExecutionLogsControllerTests
{
    private readonly IExecutionService _executions = Substitute.For<IExecutionService>();
    private readonly IProcedureService _procedures = Substitute.For<IProcedureService>();
    private readonly ExecutionLogsController _sut;

    public ExecutionLogsControllerTests()
    {
        _procedures.ListAllAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new ProcedureLookupDto
            {
                Id = 1,
                CategoryId = 1,
                Caption = "Invoices - List",
                RoleEntitlement = "user"
            }
        ]);

        _sut = new ExecutionLogsController(_executions, _procedures);
    }

    [Fact]
    public async Task Index_returns_view_with_search_results()
    {
        _executions.SearchAsync(Arg.Any<ExecutionLogFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ExecutionLogListItemDto>
            {
                Items =
                [
                    new ExecutionLogListItemDto
                    {
                        Id = 1,
                        ProcedureId = 1,
                        ProcedureCaption = "Invoices - List",
                        Username = "demo",
                        ExecutionStart = DateTime.UtcNow,
                        Success = true
                    }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 20
            });

        var result = await _sut.Index(username: "demo", procedureId: null, success: null,
            startFrom: null, startTo: null, pageNumber: 1, pageSize: 20);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ExecutionLogIndexViewModel>().Subject;
        model.Items.Should().ContainSingle(i => i.Username == "demo");
        model.Username.Should().Be("demo");
        model.Pager.Controller.Should().Be("ExecutionLogs");
        model.ProcedureOptions.Should().Contain(o => o.Text == "Invoices - List");
    }

    [Fact]
    public async Task Index_passes_filters_through_to_the_service()
    {
        _executions.SearchAsync(Arg.Any<ExecutionLogFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ExecutionLogListItemDto>
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 20
            });

        var startFrom = new DateTime(2026, 7, 1);
        var startTo = new DateTime(2026, 7, 20);

        await _sut.Index(username: "demo", procedureId: 1, success: false,
            startFrom: startFrom, startTo: startTo, pageNumber: 1, pageSize: 20);

        await _executions.Received(1).SearchAsync(
            Arg.Is<ExecutionLogFilterDto>(f =>
                f.Username == "demo"
                && f.ProcedureId == 1
                && f.Success == false
                && f.StartFrom == startFrom
                && f.StartTo == startTo),
            Arg.Any<CancellationToken>());
    }
}
