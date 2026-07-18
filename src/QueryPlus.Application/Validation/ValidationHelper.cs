using FluentValidation;
using FluentValidation.Results;
using AppValidationException = QueryPlus.Application.Common.ValidationException;

namespace QueryPlus.Application.Validation;

public static class ValidationHelper
{
    public static async Task ValidateAndThrowAsync<T>(
        IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw ToException(result);
        }
    }

    public static AppValidationException ToException(ValidationResult result)
    {
        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        return new AppValidationException(errors);
    }
}
