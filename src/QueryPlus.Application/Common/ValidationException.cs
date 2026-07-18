namespace QueryPlus.Application.Common;

public sealed class ValidationException : Exception
{
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string errorMessage)
        : this(new Dictionary<string, string[]>
        {
            [propertyName] = [errorMessage]
        })
    {
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
