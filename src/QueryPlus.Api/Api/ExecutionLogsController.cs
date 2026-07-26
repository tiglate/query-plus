using Microsoft.AspNetCore.Mvc;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Interfaces;

namespace QueryPlus.Api.Api;

[ApiController]
[Route("api/execution-logs")]
public sealed class ExecutionLogsController(IExecutionService executions, IProcedureService procedures) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<ExecutionLogListItemDto>> Search(string? username, int? procedureId, bool? success,
        DateTime? startFrom, DateTime? startTo, int pageNumber = 1,
        int pageSize = PagedResult<ExecutionLogListItemDto>.DefaultPageSize,
        CancellationToken cancellationToken = default) => executions.SearchAsync(
        new()
        {
            Username = username, ProcedureId = procedureId, Success = success, StartFrom = startFrom, StartTo = startTo,
            Page = pageNumber, PageSize = pageSize
        }, cancellationToken);

    [HttpGet("procedures")]
    public Task<IReadOnlyList<ProcedureLookupDto>> ProcedureLookup(CancellationToken cancellationToken) =>
        procedures.ListAllAsync(cancellationToken);
}
