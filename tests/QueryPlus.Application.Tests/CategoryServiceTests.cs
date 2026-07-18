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
}
