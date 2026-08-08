using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QueryPlus.Api.DependencyInjection;
using QueryPlus.Api.Security;
using QueryPlus.Api.Services;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Common;
using QueryPlus.Application.Interfaces;

namespace QueryPlus.Api.Api;

[ApiController]
[Route("api/exports")]
[Authorize(Roles = AppRoles.CanExecute)]
public sealed class ExportsController(
    IExcelExportService exports,
    IProcedureService procedures,
    ExportEligibilityService eligibility,
    ICurrentUserContext user) : ControllerBase
{
    public sealed class ExportRequest
    {
        public int ProcedureId { get; init; }

        public IDictionary<string, string?> ParameterValues { get; init; } =
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitingServiceCollectionExtensions.ExportPolicy)]
    public async Task<IActionResult> Queue(ExportRequest request, CancellationToken cancellationToken)
    {
        var values = Normalize(request.ParameterValues, out var reserved);
        if (reserved.Count > 0)
            return Problem(title: "Reserved pagination parameters are not accepted",
                detail: string.Join(", ", reserved), statusCode: 400);
        if (!eligibility.TryValidate(user.Username, request.ProcedureId, values, out var reason))
            return Problem(title: "Export is not eligible", detail: reason, statusCode: 400);
        var procedure = await procedures.GetByIdAsync(request.ProcedureId, cancellationToken);
        if (procedure is null || !procedure.Enabled)
        {
            eligibility.Clear(user.Username);
            return Problem(title: "Procedure not found or disabled", statusCode: 404);
        }

        var id = exports.QueueExport(request.ProcedureId, values, user.Username, user.Roles);
        return AcceptedAtAction(nameof(Status), new { jobId = id }, exports.GetJob(id));
    }

    [HttpGet("{jobId:guid}")]
    public IActionResult Status(Guid jobId)
    {
        var job = exports.GetJob(jobId);
        if (job is null || !Owned(job)) return Problem(title: "Export job not found", statusCode: 404);
        return Ok(job);
    }

    [HttpGet("{jobId:guid}/download")]
    public IActionResult Download(Guid jobId)
    {
        var job = exports.GetJob(jobId);
        if (job is null || !Owned(job)) return Problem(title: "Export job not found", statusCode: 404);
        var path = exports.GetFilePath(jobId);
        return path is null
            ? Problem(title: "Export file is not available", statusCode: 404)
            : PhysicalFile(path, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                job.FileName ?? Path.GetFileName(path));
    }

    private bool Owned(ExportJobDto job) =>
        string.Equals(job.Username, user.Username, StringComparison.OrdinalIgnoreCase);

    private static Dictionary<string, string?> Normalize(IDictionary<string, string?> source, out List<string> reserved)
    {
        reserved = [];
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in source)
        {
            var name = pair.Key.Trim().TrimStart('@');
            if (ProcedurePagination.IsReservedParameterName(name))
            {
                reserved.Add(pair.Key);
                continue;
            }

            if (name.Length > 0) values[name] = pair.Value?.Trim();
        }

        return values;
    }
}
