using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using NSubstitute;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Interfaces;
using QueryPlus.Web.Controllers.Admin;
using QueryPlus.Web.Resources;
using QueryPlus.Web.ViewModels;

namespace QueryPlus.Web.Tests.Controllers;

public sealed class ProceduresControllerTests
{
    private readonly IProcedureService _procedures = Substitute.For<IProcedureService>();
    private readonly ICategoryService _categories = Substitute.For<ICategoryService>();
    private readonly IProcedureMetadataSyncService _metadata = Substitute.For<IProcedureMetadataSyncService>();
    private readonly IStringLocalizer<SharedResource> _localizer = Substitute.For<IStringLocalizer<SharedResource>>();
    private readonly ProceduresController _sut;

    public ProceduresControllerTests()
    {
        _localizer[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.Arg<string>(), call.Arg<string>()));

        _categories.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(
            [
                new CategoryListItemDto
                {
                    Id = 1,
                    Description = "Sales",
                    CreatedAt = DateTime.UtcNow
                }
            ]);

        _sut = new ProceduresController(_procedures, _categories, _metadata, _localizer)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Substitute.For<ITempDataProvider>()),
            ObjectValidator = new NoOpObjectModelValidator()
        };
    }

    private sealed class NoOpObjectModelValidator : IObjectModelValidator
    {
        public void Validate(
            ActionContext actionContext,
            ValidationStateDictionary? validationState,
            string prefix,
            object? model)
        {
            // Unit tests supply valid models; skip full graph validation plumbing.
        }
    }

    [Fact]
    public async Task Index_returns_view_with_search_results()
    {
        _procedures.SearchAsync(Arg.Any<ProcedureFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProcedureListItemDto>
            {
                Items =
                [
                    new ProcedureListItemDto
                    {
                        Id = 5,
                        CategoryId = 1,
                        Caption = "Sales Report",
                        DatabaseName = "db",
                        ProcedureName = "dbo.usp_Sales",
                        RoleEntitlement = "user",
                        Enabled = true,
                        CreatedAt = DateTime.UtcNow
                    }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 20
            });

        var result = await _sut.Index(
            categoryId: 1,
            caption: "Sales",
            roleEntitlement: null,
            enabled: null);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ProcedureIndexViewModel>().Subject;
        model.Items.Should().ContainSingle(i => i.Caption == "Sales Report");
        model.CategoryId.Should().Be(1);
        model.Pager.Controller.Should().Be("Procedures");
        model.CategoryOptions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Details_returns_not_found_when_missing()
    {
        _procedures.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((ProcedureDetailDto?)null);

        var result = await _sut.Details(99);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Details_returns_read_only_view_model()
    {
        _procedures.GetByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(new ProcedureDetailDto
            {
                Id = 5,
                CategoryId = 1,
                Caption = "Sales Report",
                DatabaseName = "db",
                ProcedureName = "dbo.usp_Sales",
                RoleEntitlement = "user",
                Enabled = true,
                CreatedBy = "creator",
                UpdatedBy = "editor"
            });

        var result = await _sut.Details(5);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ProcedureEditViewModel>().Subject;
        model.Id.Should().Be(5);
        model.ReadOnly.Should().BeTrue();
        model.Caption.Should().Be("Sales Report");
        model.CreatedBy.Should().Be("creator");
        model.UpdatedBy.Should().Be("editor");
    }

    [Fact]
    public async Task Edit_returns_audit_details()
    {
        _procedures.GetByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(new ProcedureDetailDto
            {
                Id = 5,
                CategoryId = 1,
                Caption = "Sales Report",
                DatabaseName = "db",
                ProcedureName = "dbo.usp_Sales",
                RoleEntitlement = "user",
                CreatedBy = "creator",
                UpdatedBy = "editor"
            });

        var result = await _sut.Edit(5);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ProcedureEditViewModel>().Subject;
        model.ReadOnly.Should().BeFalse();
        model.CreatedBy.Should().Be("creator");
        model.UpdatedBy.Should().Be("editor");
    }

    [Fact]
    public async Task Create_get_returns_empty_enabled_form()
    {
        var result = await _sut.Create();

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ProcedureEditViewModel>().Subject;
        model.Enabled.Should().BeTrue();
        model.Id.Should().BeNull();
    }

    [Fact]
    public async Task Create_post_redirects_to_details_on_success()
    {
        _procedures.CreateAsync(Arg.Any<SaveProcedureDto>(), Arg.Any<CancellationToken>())
            .Returns(new ProcedureDetailDto
            {
                Id = 42,
                CategoryId = 1,
                Caption = "New Proc",
                DatabaseName = "db",
                ProcedureName = "dbo.usp_New",
                RoleEntitlement = "user",
                Enabled = true
            });

        var input = new ProcedureEditViewModel
        {
            CategoryId = 1,
            Caption = "New Proc",
            DatabaseName = "db",
            ProcedureName = "dbo.usp_New",
            RoleEntitlement = "user",
            Enabled = true
        };

        var result = await _sut.Create(input);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(ProceduresController.Details));
        redirect.RouteValues!["id"].Should().Be(42);
        await _procedures.Received(1).CreateAsync(Arg.Any<SaveProcedureDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_calls_service_and_redirects()
    {
        var result = await _sut.Delete(id: 9, categoryId: null, caption: null, roleEntitlement: null, enabled: null);

        await _procedures.Received(1).DeleteAsync(9, Arg.Any<CancellationToken>());
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(ProceduresController.Index));
        _sut.TempData["Success"].Should().NotBeNull();
    }

    [Fact]
    public async Task AddParameter_appends_row_and_redisplays_create()
    {
        var input = new ProcedureEditViewModel
        {
            Caption = "Draft",
            DatabaseName = "db",
            ProcedureName = "dbo.x",
            RoleEntitlement = "user",
            Parameters = []
        };

        var result = await _sut.AddParameter(id: null, input);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.ViewName.Should().Be("Create");
        var model = view.Model.Should().BeOfType<ProcedureEditViewModel>().Subject;
        model.Parameters.Should().ContainSingle(p => p.Name == "@Param");
    }

    [Fact]
    public async Task SyncMetadata_requires_database_and_procedure()
    {
        var input = new ProcedureEditViewModel
        {
            Caption = "Draft",
            DatabaseName = "",
            ProcedureName = "",
            RoleEntitlement = "user"
        };

        var result = await _sut.SyncMetadata(id: null, input);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.ViewName.Should().Be("Create");
        _sut.TempData["Error"].Should().NotBeNull();
        await _metadata.DidNotReceive()
            .FetchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncMetadata_applies_snapshot_when_source_set()
    {
        _metadata.FetchAsync("QueryPlus", "dbo.usp_X", Arg.Any<CancellationToken>())
            .Returns(new ProcedureMetadataSnapshot
            {
                Parameters =
                [
                    new SaveProcedureParameterDto
                    {
                        Caption = "Start",
                        Name = "@Start",
                        ParameterType = Domain.Enums.ParameterType.Date
                    }
                ],
                Columns =
                [
                    new SaveProcedureColumnDto
                    {
                        TechnicalName = "Id",
                        Caption = "Id"
                    }
                ]
            });

        var input = new ProcedureEditViewModel
        {
            Caption = "Draft",
            DatabaseName = "QueryPlus",
            ProcedureName = "dbo.usp_X",
            RoleEntitlement = "user"
        };

        var result = await _sut.SyncMetadata(id: null, input);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ProcedureEditViewModel>().Subject;
        model.Parameters.Should().ContainSingle(p => p.Name == "@Start");
        model.Columns.Should().ContainSingle(c => c.TechnicalName == "Id");
        _sut.TempData["Success"].Should().NotBeNull();
    }
}
