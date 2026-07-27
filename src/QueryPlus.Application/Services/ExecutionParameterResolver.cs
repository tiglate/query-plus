using QueryPlus.Application.Common;
using QueryPlus.Application.Services.Converters;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Application.Services;

public sealed class ExecutionParameterResolver : IExecutionParameterResolver
{
    private readonly IParameterConverterRegistry _converterRegistry;

    public ExecutionParameterResolver(IParameterConverterRegistry converterRegistry)
    {
        _converterRegistry = converterRegistry;
    }

    public ResolvedExecutionParameters Resolve(
        Procedure procedure,
        IDictionary<string, string?> rawValues,
        long? requestedPageNumber,
        long? requestedPageSize)
    {
        var userParameterDefs = procedure.Parameters
            .Where(p => !ProcedurePagination.IsReservedParameterName(p.Name))
            .ToList();

        var boundParameters = ParameterValueBinder.Bind(userParameterDefs, rawValues, _converterRegistry);

        var pageNumber = ProcedurePagination.DefaultPageNumber;
        var pageSize = ProcedurePagination.DefaultPageSize;
        IReadOnlyDictionary<string, object?> execParameters = boundParameters;
        IReadOnlyCollection<string>? outputs = null;

        if (procedure.SupportsPagination)
        {
            pageNumber = ProcedurePagination.ClampPageNumber(requestedPageNumber);
            pageSize = ProcedurePagination.ClampUiPageSize(requestedPageSize);
            execParameters = ProcedurePagination.WithPagingInputs(
                boundParameters,
                pageNumber,
                pageSize);
            outputs = [ProcedurePagination.TotalRecordsName];
        }

        return new ResolvedExecutionParameters(
            BoundUserParameters: boundParameters,
            ExecParameters: execParameters,
            OutputParameterNames: outputs,
            PageNumber: pageNumber,
            PageSize: pageSize);
    }
}
