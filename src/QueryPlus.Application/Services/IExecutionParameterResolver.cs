using QueryPlus.Domain.Entities;

namespace QueryPlus.Application.Services;

public record ResolvedExecutionParameters(
    IReadOnlyDictionary<string, object?> BoundUserParameters,
    IReadOnlyDictionary<string, object?> ExecParameters,
    IReadOnlyCollection<string>? OutputParameterNames,
    long PageNumber,
    long PageSize);

public interface IExecutionParameterResolver
{
    ResolvedExecutionParameters Resolve(
        Procedure procedure,
        IDictionary<string, string?> rawValues,
        long? requestedPageNumber,
        long? requestedPageSize);
}
