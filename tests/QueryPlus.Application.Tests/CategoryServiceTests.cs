using FluentAssertions;
using NSubstitute;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.Interfaces;
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
    private readonly IConfigurationAuditReader _auditReader = Substitute.For<IConfigurationAuditReader>();
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _auditReader.GetCategoryAuditDetailsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new AuditDetailsDto { CreatedBy = "admin", UpdatedBy = "admin" });

        _sut = new CategoryService(
            _categories,
            _unitOfWork,
            _auditReader,
            new CreateCategoryDtoValidator(),
            new UpdateCategoryDtoValidator());
    }

    [Fact]
    public async Task SearchAsync_ReturnsPagedCategories()
    {
        var list = new List<Category> { new() { IdCategory = 1, Description = "Finance" } };
        _categories.SearchAsync("Fin", 1, 10, Arg.Any<CancellationToken>()).Returns((list, 1));

        var result = await _sut.SearchAsync(new CategoryFilterDto { Description = "Fin", Page = 1, PageSize = 10 });

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.Description == "Finance");
    }

    [Fact]
    public async Task SearchAsync_WhenRequestedPageIsPastTheEnd_ClampsAndRefetchesOnce()
    {
        // Page 5 of a 10-item, 10-per-page result set is past the end (only 1 page exists).
        var emptyPastEnd = new List<Category>();
        var clampedPage = new List<Category> { new() { IdCategory = 1, Description = "Finance" } };
        _categories.SearchAsync("Fin", 5, 10, Arg.Any<CancellationToken>()).Returns((emptyPastEnd, 10));
        _categories.SearchAsync("Fin", 1, 10, Arg.Any<CancellationToken>()).Returns((clampedPage, 10));

        var result = await _sut.SearchAsync(new CategoryFilterDto { Description = "Fin", Page = 5, PageSize = 10 });

        result.Page.Should().Be(1);
        result.TotalCount.Should().Be(10);
        result.Items.Should().ContainSingle(i => i.Description == "Finance");
        await _categories.Received(1).SearchAsync("Fin", 5, 10, Arg.Any<CancellationToken>());
        await _categories.Received(1).SearchAsync("Fin", 1, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAllAsync_ReturnsAllCategoriesAsDtos()
    {
        _categories.GetAllAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new Category { IdCategory = 1, Description = "Finance" },
            new Category { IdCategory = 2, Description = "Sales" }
        ]);

        var result = await _sut.ListAllAsync();

        result.Should().HaveCount(2);
        result.Select(i => i.Description).Should().BeEquivalentTo(["Finance", "Sales"]);
    }

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsDetailDtoWithAuditInfo()
    {
        _categories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Category { IdCategory = 1, Description = "Finance" });

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Description.Should().Be("Finance");
        result.CreatedBy.Should().Be("admin");
        result.UpdatedBy.Should().Be("admin");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _categories.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var result = await _sut.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_DuplicateDescription_ThrowsValidationException()
    {
        _categories.ExistsByDescriptionAsync("Finance", null, Arg.Any<CancellationToken>()).Returns(true);

        var dto = new CreateCategoryDto { Description = "Finance" };

        Func<Task> act = async () => await _sut.CreateAsync(dto);

        var exc = await act.Should().ThrowAsync<Common.ValidationException>();
        exc.Which.Errors.Should().ContainKey("Description");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesCategory()
    {
        _categories.ExistsByDescriptionAsync("Sales", null, Arg.Any<CancellationToken>()).Returns(false);

        var dto = new CreateCategoryDto { Description = "Sales" };

        var result = await _sut.CreateAsync(dto);

        result.Description.Should().Be("Sales");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsEntityNotFoundException()
    {
        _categories.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var dto = new UpdateCategoryDto { Id = 999, Description = "Updated" };

        Func<Task> act = async () => await _sut.UpdateAsync(dto);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_UpdatesDescription()
    {
        var entity = new Category { IdCategory = 1, Description = "Old Name" };
        _categories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);
        _categories.ExistsByDescriptionAsync("New Name", 1, Arg.Any<CancellationToken>()).Returns(false);

        var dto = new UpdateCategoryDto { Id = 1, Description = "New Name" };

        var result = await _sut.UpdateAsync(dto);

        result.Description.Should().Be("New Name");
        entity.Description.Should().Be("New Name");
        _categories.Received(1).Update(entity);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_DuplicateDescription_ThrowsValidationException()
    {
        _categories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Category { IdCategory = 1, Description = "Old Name" });
        _categories.ExistsByDescriptionAsync("Sales", 1, Arg.Any<CancellationToken>()).Returns(true);

        var dto = new UpdateCategoryDto { Id = 1, Description = "Sales" };

        Func<Task> act = async () => await _sut.UpdateAsync(dto);

        var exc = await act.Should().ThrowAsync<Common.ValidationException>();
        exc.Which.Errors.Should().ContainKey("Description");
    }

    [Fact]
    public async Task DeleteAsync_HasProcedures_ThrowsBusinessRuleException()
    {
        _categories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Category { IdCategory = 1, Description = "Ops" });
        _categories.HasProceduresAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        Func<Task> act = async () => await _sut.DeleteAsync(1);

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*still has procedures*");
    }

    [Fact]
    public async Task DeleteAsync_NoProcedures_RemovesCategory()
    {
        var entity = new Category { IdCategory = 1, Description = "Ops" };
        _categories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);
        _categories.HasProceduresAsync(1, Arg.Any<CancellationToken>()).Returns(false);

        await _sut.DeleteAsync(1);

        _categories.Received(1).Remove(entity);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
