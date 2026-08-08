using FluentAssertions;
using FluentValidation.Results;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.Validation;
using AppValidationException = QueryPlus.Application.Common.ValidationException;

namespace QueryPlus.Application.Tests;

public class ValidationHelperTests
{
    [Fact]
    public async Task ValidateAndThrowAsync_ValidInstance_DoesNotThrow()
    {
        var validator = new CreateCategoryDtoValidator();
        var dto = new CreateCategoryDto { Description = "Finance" };

        var act = async () => await ValidationHelper.ValidateAndThrowAsync(validator, dto);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateAndThrowAsync_InvalidInstance_ThrowsMappedValidationException()
    {
        var validator = new CreateCategoryDtoValidator();
        var dto = new CreateCategoryDto { Description = "" };

        var act = async () => await ValidationHelper.ValidateAndThrowAsync(validator, dto);

        var exception = await act.Should().ThrowAsync<AppValidationException>();
        exception.Which.Errors.Should().ContainKey("Description");
    }

    [Fact]
    public void ToException_GroupsErrorsByPropertyName_AndDedupesMessages()
    {
        var result = new ValidationResult(
        [
            new ValidationFailure("Description", "Description is required."),
            new ValidationFailure("Description", "Description is too long."),
            new ValidationFailure("Description", "Description is required."), // duplicate message
            new ValidationFailure("Page", "Page must be positive.")
        ]);

        var exception = ValidationHelper.ToException(result);

        exception.Errors.Should().HaveCount(2);
        exception.Errors["Description"].Should().BeEquivalentTo(
            ["Description is required.", "Description is too long."]);
        exception.Errors["Page"].Should().BeEquivalentTo(["Page must be positive."]);
    }
}
