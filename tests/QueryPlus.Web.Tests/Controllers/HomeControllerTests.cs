using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NSubstitute;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Interfaces;
using QueryPlus.Domain.Interfaces;
using QueryPlus.Web.Controllers;
using QueryPlus.Web.Resources;
using QueryPlus.Web.Services;
using QueryPlus.Web.ViewModels;

namespace QueryPlus.Web.Tests.Controllers;

public sealed class HomeControllerTests
{
    private readonly IProcedureService _procedures = Substitute.For<IProcedureService>();
    private readonly IProcedureRepository _procedureRepository = Substitute.For<IProcedureRepository>();
    private readonly IExecutionService _execution = Substitute.For<IExecutionService>();
    private readonly IExcelExportService _exports = Substitute.For<IExcelExportService>();
    private readonly ExportEligibilityService _eligibility = new();
    private readonly ICurrentUserContext _user = Substitute.For<ICurrentUserContext>();
    private readonly IStringLocalizer<SharedResource> _localizer = Substitute.For<IStringLocalizer<SharedResource>>();
    private readonly HomeController _sut;

    public HomeControllerTests()
    {
        _user.Username.Returns("tester");
        _localizer[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.Arg<string>(), call.Arg<string>()));

        _sut = new HomeController(
            _procedures,
            _procedureRepository,
            _execution,
            _exports,
            _eligibility,
            _user,
            _localizer)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task Index_loads_accessible_procedures()
    {
        _procedures.GetAccessibleForCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(
            [
                new ProcedureLookupDto
                {
                    Id = 7,
                    CategoryId = 1,
                    Caption = "Sales",
                    RoleEntitlement = "user"
                }
            ]);

        var result = await _sut.Index(procedureId: null);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<HomeIndexViewModel>().Subject;
        model.AccessibleProcedures.Should().ContainSingle(p => p.Id == 7);
        model.SelectedProcedure.Should().BeNull();
    }

    [Fact]
    public async Task Index_selects_procedure_when_accessible()
    {
        _procedures.GetAccessibleForCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(
            [
                new ProcedureLookupDto
                {
                    Id = 7,
                    CategoryId = 1,
                    Caption = "Sales",
                    RoleEntitlement = "user"
                }
            ]);
        _procedures.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(new ProcedureDetailDto
            {
                Id = 7,
                CategoryId = 1,
                Caption = "Sales",
                DatabaseName = "db",
                ProcedureName = "dbo.usp",
                RoleEntitlement = "user",
                Enabled = true
            });

        var result = await _sut.Index(procedureId: 7);

        var model = result.Should().BeOfType<ViewResult>().Subject.Model
            .Should().BeOfType<HomeIndexViewModel>().Subject;
        model.ProcedureId.Should().Be(7);
        model.SelectedProcedure!.Caption.Should().Be("Sales");
    }

    [Fact]
    public async Task Parameters_returns_html_when_no_procedure()
    {
        var result = await _sut.Parameters(procedureId: null);

        var content = result.Should().BeOfType<ContentResult>().Subject;
        content.ContentType.Should().Be("text/html");
        content.Content.Should().Contain("Home_NoProcedure");
    }

    [Fact]
    public async Task Parameters_returns_partial_when_found()
    {
        _procedures.GetByIdAsync(3, Arg.Any<CancellationToken>())
            .Returns(new ProcedureDetailDto
            {
                Id = 3,
                CategoryId = 1,
                Caption = "X",
                DatabaseName = "db",
                ProcedureName = "dbo.x",
                RoleEntitlement = "user",
                Enabled = true
            });

        var result = await _sut.Parameters(3);

        result.Should().BeOfType<PartialViewResult>()
            .Which.Model.Should().BeOfType<ProcedureDetailDto>()
            .Which.Id.Should().Be(3);
    }

    [Fact]
    public async Task Execute_without_procedure_returns_error_grid()
    {
        var result = await _sut.Execute(procedureId: null);

        var partial = result.Should().BeOfType<PartialViewResult>().Subject;
        var model = partial.Model.Should().BeOfType<ExecutionResultDto>().Subject;
        model.Success.Should().BeFalse();
    }
}
