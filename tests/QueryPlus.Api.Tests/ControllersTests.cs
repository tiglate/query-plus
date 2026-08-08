using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using QueryPlus.Api.Api;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.Interfaces;

namespace QueryPlus.Api.Tests;

public class ControllersTests
{
    private readonly ICategoryService _categories = Substitute.For<ICategoryService>();
    private readonly IProcedureService _procedures = Substitute.For<IProcedureService>();
    private readonly ICurrentUserContext _user = Substitute.For<ICurrentUserContext>();

    [Fact]
    public void HealthController_Get_ReturnsHealthy()
    {
        var controller = new HealthController();

        var result = controller.Get() as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task CategoriesController_Search_ReturnsOk()
    {
        var controller = new CategoriesController(_categories);
        var expected = new PagedResult<CategoryListItemDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 10 };
        _categories.SearchAsync(Arg.Any<CategoryFilterDto>(), Arg.Any<CancellationToken>()).Returns(expected);

        var result = await controller.Search(description: "Fin", pageNumber: 1, pageSize: 10);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CategoriesController_Get_ReturnsNotFound_WhenNull()
    {
        var controller = new CategoriesController(_categories);
        _categories.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((CategoryDetailDto?)null);

        var actionResult = await controller.Get(999, CancellationToken.None);

        // Must be a single, flat ObjectResult (from Problem()) - not NotFound(Problem(...)),
        // which double-wraps the ProblemDetails inside another ObjectResult's Value and
        // corrupts the serialized response body.
        var result = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(404);
        result.Value.Should().BeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>();
    }

    [Fact]
    public async Task CategoriesController_Delete_ReturnsNoContent_WhenFound()
    {
        var controller = new CategoriesController(_categories);
        _categories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new CategoryDetailDto { Id = 1, Description = "Test" });

        var actionResult = await controller.Delete(1, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        await _categories.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AuthController_GetUser_ReturnsOkWithUserInfo()
    {
        _user.Username.Returns("john");
        _user.Roles.Returns(["admin"]);
        _user.IsAuthenticated.Returns(true);

        var controller = new AuthController(_user);

        var result = controller.GetUser() as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }
}
