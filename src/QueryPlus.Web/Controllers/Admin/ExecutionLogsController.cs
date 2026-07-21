using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.Interfaces;
using QueryPlus.Web.Models;
using QueryPlus.Web.ViewModels;

namespace QueryPlus.Web.Controllers.Admin;

[Route("Admin/ExecutionLogs")]
public sealed class ExecutionLogsController(
    IExecutionService executions,
    IProcedureService procedures) : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(
        string? username,
        int? procedureId,
        bool? success,
        DateTime? startFrom,
        DateTime? startTo,
        int pageNumber = 1,
        int pageSize = 0,
        CancellationToken cancellationToken = default)
    {
        ViewData["PageKey"] = "admin-execution-logs";
        if (pageSize <= 0)
        {
            pageSize = PagedResult<ExecutionLogListItemDto>.DefaultPageSize;
        }

        var procedureOptions = await BuildProcedureFilterOptionsAsync(procedureId, cancellationToken);

        var result = await executions.SearchAsync(new ExecutionLogFilterDto
        {
            Username = username,
            ProcedureId = procedureId,
            Success = success,
            StartFrom = startFrom,
            StartTo = startTo,
            Page = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        var pager = new PagerModel
        {
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages,
            Controller = "ExecutionLogs",
            Action = "Index",
            RouteValues = BuildRouteValues(username, procedureId, success, startFrom, startTo, result.PageSize)
        };

        return View(new ExecutionLogIndexViewModel
        {
            Username = username,
            ProcedureId = procedureId,
            Success = success,
            StartFrom = startFrom,
            StartTo = startTo,
            PageNumber = result.Page,
            PageSize = result.PageSize,
            Result = result,
            ProcedureOptions = procedureOptions,
            Pager = pager
        });
    }

    private async Task<List<SelectListItem>> BuildProcedureFilterOptionsAsync(
        int? procedureId,
        CancellationToken cancellationToken = default)
    {
        var procs = await procedures.ListAllAsync(cancellationToken);
        var options = procs
            .Select(p => new SelectListItem(p.Caption, p.Id.ToString(), procedureId == p.Id))
            .ToList();
        options.Insert(0, new SelectListItem("—", ""));
        return options;
    }

    private static Dictionary<string, string?> BuildRouteValues(
        string? username,
        int? procedureId,
        bool? success,
        DateTime? startFrom,
        DateTime? startTo,
        int pageSize)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(username))
        {
            values["username"] = username;
        }

        if (procedureId is not null)
        {
            values["procedureId"] = procedureId.Value.ToString();
        }

        if (success is not null)
        {
            values["success"] = success.Value ? "true" : "false";
        }

        if (startFrom is not null)
        {
            values["startFrom"] = startFrom.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (startTo is not null)
        {
            values["startTo"] = startTo.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (pageSize != PagedResult<ExecutionLogListItemDto>.DefaultPageSize)
        {
            values["pageSize"] = pageSize.ToString();
        }

        return values;
    }
}
