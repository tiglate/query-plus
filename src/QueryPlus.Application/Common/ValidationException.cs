namespace QueryPlus.Application.Common;

public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public ValidationException(string propertyName, string errorMessage)
        : this(new Dictionary<string, string[]>
        {
            [propertyName] = [errorMessage]
        })
    {
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
