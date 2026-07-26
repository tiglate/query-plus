using Microsoft.AspNetCore.Mvc;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Interfaces;

namespace QueryPlus.Api.Api;

[ApiController]
[Route("api/procedures")]
public sealed class ProceduresController(IProcedureService procedures, IProcedureMetadataSyncService metadataSync)
    : ControllerBase
{
    public sealed record SyncMetadataRequest(string DatabaseName, string ProcedureName);

    [HttpGet("accessible")]
    public Task<IReadOnlyList<ProcedureLookupDto>> Accessible(CancellationToken cancellationToken) =>
        procedures.GetAccessibleForCurrentUserAsync(cancellationToken);

    [HttpGet]
    public Task<PagedResult<ProcedureListItemDto>> Search(int? categoryId, string? caption, string? roleEntitlement,
        bool? enabled, string? databaseName, string? procedureName, int pageNumber = 1,
        int pageSize = PagedResult<ProcedureListItemDto>.DefaultPageSize,
        CancellationToken cancellationToken = default) => procedures.SearchAsync(
        new ProcedureFilterDto
        {
            CategoryId = categoryId, Caption = caption, RoleEntitlement = roleEntitlement, Enabled = enabled,
            DatabaseName = databaseName, ProcedureName = procedureName, Page = pageNumber, PageSize = pageSize
        }, cancellationToken);

    [HttpGet("lookup")]
    public Task<IReadOnlyList<ProcedureLookupDto>> Lookup(CancellationToken cancellationToken) =>
        procedures.ListAllAsync(cancellationToken);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProcedureDetailDto>> Get(int id, CancellationToken cancellationToken)
    {
        var result = await procedures.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound(Problem(title: "Procedure not found", statusCode: 404)) : Ok(result);
    }

    [HttpGet("{id:int}/parameters")]
    public async Task<IActionResult> Parameters(int id, CancellationToken cancellationToken)
    {
        var result = await procedures.GetByIdAsync(id, cancellationToken);
        return result is null
            ? NotFound(Problem(title: "Procedure not found", statusCode: 404))
            : Ok(result.Parameters);
    }

    [HttpPost]
    public async Task<ActionResult<ProcedureDetailDto>> Create(SaveProcedureDto request,
        CancellationToken cancellationToken)
    {
        var created = await procedures.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProcedureDetailDto>> Update(int id, SaveProcedureDto request,
        CancellationToken cancellationToken)
    {
        if (await procedures.GetByIdAsync(id, cancellationToken) is null)
            return NotFound(Problem(title: "Procedure not found", statusCode: 404));
        return Ok(await procedures.UpdateAsync(CopyWithId(request, id), cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (await procedures.GetByIdAsync(id, cancellationToken) is null)
            return NotFound(Problem(title: "Procedure not found", statusCode: 404));
        await procedures.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/sync-metadata")]
    public async Task<IActionResult> SyncMetadata(int id, SyncMetadataRequest request,
        CancellationToken cancellationToken)
    {
        if (id > 0 && await procedures.GetByIdAsync(id, cancellationToken) is null)
        {
            return NotFound(Problem(title: "Procedure not found", statusCode: 404));
        }

        if (string.IsNullOrWhiteSpace(request.DatabaseName) || string.IsNullOrWhiteSpace(request.ProcedureName))
        {
            return BadRequest(Problem(title: "Database and procedure names are required", statusCode: 400));
        }

        return Ok(await metadataSync.FetchAsync(request.DatabaseName, request.ProcedureName, cancellationToken));
    }

    private static SaveProcedureDto CopyWithId(SaveProcedureDto x, int id) => new()
    {
        Id = id, CategoryId = x.CategoryId, Caption = x.Caption, DatabaseName = x.DatabaseName,
        ProcedureName = x.ProcedureName, Enabled = x.Enabled, SupportsPagination = x.SupportsPagination,
        RoleEntitlement = x.RoleEntitlement, Description = x.Description, Parameters = x.Parameters, Columns = x.Columns
    };
}
