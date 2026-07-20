namespace QueryPlus.Domain.Exceptions;

public sealed class ForbiddenOperationException(string message) : DomainException(message);
