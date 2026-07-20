namespace QueryPlus.Domain.Exceptions;

public sealed class EntityNotFoundException(string entityName, int id)
    : DomainException($"{entityName} with id {id} was not found.")
{
    public string EntityName { get; } = entityName;
    public int Id { get; } = id;
}
