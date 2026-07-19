using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.Mapping;
using QueryPlus.Application.Services;
using QueryPlus.Application.Validation;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Application.Tests;

public class CategoryServiceTests
{
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        var mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<QueryPlusMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        _sut = new CategoryService(
            _categories,
            _unitOfWork,
            mapper,
            new CreateCategoryDtoValidator(),
            new UpdateCategoryDtoValidator());
    }

    [Fact]
    public async Task CreateAsync_PersistsCategory()
    {
        _categories.ExistsByDescriptionAsync("Finance", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CreateAsync(new CreateCategoryDto { Description = "Finance" });

        result.Description.Should().Be("Finance");
        await _categories.Received(1).AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenHasProcedures()
    {
        _categories.GetByIdAsync(1).Returns(new Category { IdCategory = 1, Description = "X" });
        _categories.HasProceduresAsync(1).Returns(true);

        var act = async () => await _sut.DeleteAsync(1);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task SearchAsync_ReturnsPagedResult()
    {
        var entities = new List<Category>
        {
            new() { IdCategory = 1, Description = "Alpha" },
            new() { IdCategory = 2, Description = "Beta" }
        };
        _categories.SearchAsync("a", 1, 20, Arg.Any<CancellationToken>())
            .Returns((entities, 2));

        var result = await _sut.SearchAsync(new CategoryFilterDto
        {
            Description = "a",
            Page = 1,
            PageSize = 20
        });

        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.Items.Should().HaveCount(2);
        result.Items[0].Description.Should().Be("Alpha");
    }

    [Fact]
    public async Task SearchAsync_ClampsPage_WhenPastEnd()
    {
        _categories.SearchAsync(null, 5, 10, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Category>(), 12));
        _categories.SearchAsync(null, 2, 10, Arg.Any<CancellationToken>())
            .Returns((
                new List<Category> { new() { IdCategory = 3, Description = "Last" } },
                12));

        var result = await _sut.SearchAsync(new CategoryFilterDto { Page = 5, PageSize = 10 });

        result.Page.Should().Be(2);
        result.TotalCount.Should().Be(12);
        result.Items.Should().ContainSingle(i => i.Description == "Last");
    }

    [Fact]
    public async Task ListAllAsync_MapsAllCategories()
    {
        _categories.GetAllAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new Category { IdCategory = 1, Description = "A" },
            new Category { IdCategory = 2, Description = "B" }
        ]);

        var result = await _sut.ListAllAsync();

        result.Should().HaveCount(2);
        result.Select(c => c.Description).Should().Equal("A", "B");
    }
}
