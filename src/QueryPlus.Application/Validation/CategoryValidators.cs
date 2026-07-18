using FluentValidation;
using QueryPlus.Application.DTOs.Categories;

namespace QueryPlus.Application.Validation;

public sealed class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(200);
    }
}

public sealed class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(200);
    }
}
