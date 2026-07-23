using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using NSubstitute;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.Interfaces;
using QueryPlus.Web.Controllers.Admin;
using QueryPlus.Web.Resources;
using QueryPlus.Web.ViewModels;

namespace QueryPlus.Web.Tests.Controllers;

public sealed class CategoriesControllerTests
{
    private readonly ICategoryService _categories = Substitute.For<ICategoryService>();
    private readonly IStringLocalizer<SharedResource> _localizer = Substitute.For<IStringLocalizer<SharedResource>>();
    private readonly CategoriesController _sut;

    public CategoriesControllerTests()
    {
        _localizer[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.Arg<string>(), call.Arg<string>()));

        _sut = new CategoriesController(_categories, _localizer)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Substitute.For<ITempDataProvider>())
        };
    }

    [Fact]
    public async Task Index_returns_view_with_search_results()
    {
        _categories.SearchAsync(Arg.Any<CategoryFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryListItemDto>
            {
                Items =
                [
                    new CategoryListItemDto
                    {
                        Id = 1,
                        Description = "Sales",
                        CreatedAt = DateTime.UtcNow
                    }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 20
            });

        var result = await _sut.Index(description: "Sal", pageNumber: 1, pageSize: 20);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<CategoryIndexViewModel>().Subject;
        model.Items.Should().ContainSingle(i => i.Description == "Sales");
        model.Description.Should().Be("Sal");
        model.Pager.Controller.Should().Be("Categories");
    }

    [Fact]
    public async Task Details_returns_not_found_when_missing()
    {
        _categories.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((CategoryDetailDto?)null);

        var result = await _sut.Details(99);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Details_returns_view_when_found()
    {
        _categories.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new CategoryDetailDto
            {
                Id = 1,
                Description = "Sales",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "creator",
                UpdatedBy = "editor"
            });

        var result = await _sut.Details(1);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<CategoryDetailDto>().Subject;
        model.Description.Should().Be("Sales");
        model.CreatedBy.Should().Be("creator");
        model.UpdatedBy.Should().Be("editor");
    }

    [Fact]
    public async Task Edit_returns_audit_details()
    {
        _categories.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new CategoryDetailDto
            {
                Id = 1,
                Description = "Sales",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "creator",
                UpdatedBy = "editor"
            });

        var result = await _sut.Edit(1);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.ViewData["CreatedBy"].Should().Be("creator");
        view.ViewData["UpdatedBy"].Should().Be("editor");
    }

    [Fact]
    public async Task Create_post_redirects_to_details_on_success()
    {
        _categories.CreateAsync(Arg.Any<CreateCategoryDto>(), Arg.Any<CancellationToken>())
            .Returns(new CategoryDetailDto
            {
                Id = 42,
                Description = "New",
                CreatedAt = DateTime.UtcNow
            });

        var result = await _sut.Create(new CategoriesController.CategoryInputModel
        {
            Description = "New"
        });

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(CategoriesController.Details));
        redirect.RouteValues!["id"].Should().Be(42);
    }

    [Fact]
    public async Task Delete_redirects_to_index_and_sets_success_tempdata()
    {
        var result = await _sut.Delete(id: 1, description: null, pageNumber: 1, pageSize: 20);

        await _categories.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(CategoriesController.Index));
        _sut.TempData["Success"].Should().NotBeNull();
    }
}
