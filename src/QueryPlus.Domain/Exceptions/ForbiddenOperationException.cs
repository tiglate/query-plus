namespace QueryPlus.Domain.Exceptions;

public sealed class ForbiddenOperationException : DomainException
{
    public ForbiddenOperationException(string message) : base(message)
    {
    }
}
