namespace QueryPlus.Domain.Exceptions;

public sealed class BusinessRuleException(string message) : DomainException(message);
