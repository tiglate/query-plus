using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Common;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Services;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Domain.Interfaces;
using QueryPlus.Web.Resources;
using QueryPlus.Web.Services;
using QueryPlus.Web.ViewModels;

namespace QueryPlus.Web.Controllers;

/// <summary>
/// Home execution screen: pick a procedure, run it, page results, export Excel.
/// </summary>
public sealed class HomeController(
    IProcedureService procedures,
    IProcedureRepository procedureRepository,
    IExecutionService execution,
    IExcelExportService exports,
    ExportEligibilityService exportEligibility,
    ICurrentUserContext user,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpGet("/")]
    [HttpGet("/Home")]
    [HttpGet("/Home/Index")]
    public async Task<IActionResult> Index(int? procedureId, CancellationToken cancellationToken = default)
    {
        ViewData["PageKey"] = "home";
        var accessible = await procedures.GetAccessibleForCurrentUserAsync(cancellationToken);
        ProcedureDetailDto? selected = null;
        var selectedId = procedureId;
        if (selectedId is > 0)
        {
            selected = await procedures.GetByIdAsync(selectedId.Value, cancellationToken);
            if (selected is null || accessible.All(p => p.Id != selectedId))
            {
                selectedId = null;
                selected = null;
            }
        }

        return View(new HomeIndexViewModel
        {
            ProcedureId = selectedId,
            AccessibleProcedures = accessible,
            SelectedProcedure = selected
        });
    }

    /// <summary>
    /// Accidental full form POST must not wipe the screen; redirect to GET.
    /// </summary>
    [HttpPost("/")]
    [HttpPost("/Home")]
    [HttpPost("/Home/Index")]
    [ValidateAntiForgeryToken]
    public IActionResult IndexPost(int? procedureId)
    {
        var id = ResolveProcedureId(procedureId);
        if (id > 0)
        {
            return RedirectToAction(nameof(Index), new { procedureId = id });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/Home/Parameters")]
    public async Task<IActionResult> Parameters(int? procedureId, CancellationToken cancellationToken = default)
    {
        exportEligibility.Clear(user.Username);

        var id = procedureId.GetValueOrDefault();
        if (id <= 0)
        {
            return Content(
                $"""
                 <p class="text-sm text-slate-500">{localizer["Home_NoProcedure"]}</p>
                 """,
                "text/html");
        }

        var procedure = await procedures.GetByIdAsync(id, cancellationToken);
        if (procedure is null)
        {
            return Content(
                $"""
                 <p class="text-sm text-slate-500">{localizer["Home_NoProcedure"]}</p>
                 """,
                "text/html");
        }

        return PartialView("Partials/_ParameterForm", procedure);
    }

    [HttpPost("/Home/Execute")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(int? procedureId, CancellationToken cancellationToken = default)
    {
        var id = ResolveProcedureId(procedureId);
        if (id <= 0)
        {
            exportEligibility.Clear(user.Username);
            return PartialView("Partials/_ResultsGrid", CreateSelectionRequiredResult());
        }

        try
        {
            var parameters = CollectParameters();

            var procedureEntity = await procedureRepository.GetEnabledByIdWithDetailsAsync(id, cancellationToken);
            if (procedureEntity is null)
            {
                exportEligibility.Clear(user.Username);
                return PartialView(
                    "~/Views/Shared/Partials/_ResultsGrid",
                    CreateErrorResult(id, localizer["Home_ProcedureNotFound"].Value));
            }

            var missing = ParameterValueBinder.GetMissingRequiredCaptions(procedureEntity.Parameters, parameters);
            if (missing.Count > 0)
            {
                exportEligibility.Clear(user.Username);
                var message = string.Format(
                    localizer["Home_RequiredParametersMissing"].Value,
                    string.Join(", ", missing));
                return PartialView(
                    "~/Views/Shared/Partials/_ResultsGrid",
                    CreateErrorResult(id, message));
            }

            var (pageNumber, pageSize) = CollectPaging();
            var result = await execution.ExecuteAsync(new ExecuteProcedureRequest
            {
                ProcedureId = id,
                ParameterValues = parameters,
                PageNumber = pageNumber,
                PageSize = pageSize
            }, cancellationToken);

            if (result.Success)
            {
                var eligibilityRows = result.SupportsPagination
                    ? (int)Math.Min(result.TotalRecords ?? result.RowCount, int.MaxValue)
                    : result.RowCount;
                if (eligibilityRows > 0)
                {
                    exportEligibility.MarkEligible(user.Username, id, parameters, eligibilityRows);
                }
                else
                {
                    exportEligibility.Clear(user.Username);
                }
            }
            else
            {
                exportEligibility.Clear(user.Username);
            }

            return PartialView("Partials/_ResultsGrid", result);
        }
        catch (ValidationException ex)
        {
            exportEligibility.Clear(user.Username);
            var message = string.Join(" ", ex.Errors.SelectMany(e => e.Value));
            return PartialView(
                "~/Views/Shared/Partials/_ResultsGrid",
                CreateErrorResult(id, message));
        }
        catch (EntityNotFoundException)
        {
            exportEligibility.Clear(user.Username);
            return PartialView(
                "~/Views/Shared/Partials/_ResultsGrid",
                CreateErrorResult(id, localizer["Home_ProcedureNotFound"].Value));
        }
        catch (ForbiddenOperationException ex)
        {
            exportEligibility.Clear(user.Username);
            return PartialView(
                "~/Views/Shared/Partials/_ResultsGrid",
                CreateErrorResult(id, ex.Message));
        }
        catch (Exception)
        {
            exportEligibility.Clear(user.Username);
            return PartialView(
                "~/Views/Shared/Partials/_ResultsGrid",
                CreateErrorResult(id, localizer["Home_ExecuteFailed"].Value));
        }
    }

    [HttpPost("/Home/Export")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Export(int? procedureId, CancellationToken cancellationToken = default)
    {
        var id = ResolveProcedureId(procedureId);
        if (id <= 0)
        {
            return Content(
                $"""
                 <span class="text-sm text-red-700">{localizer["Home_SelectProcedureRequired"]}</span>
                 """,
                "text/html");
        }

        var parameters = CollectParameters();
        if (!exportEligibility.TryValidate(user.Username, id, parameters, out var reason))
        {
            var message = reason switch
            {
                "export-no-rows" => localizer["Home_ExportRequiresData"].Value,
                "export-params-mismatch" => localizer["Home_ExportParamsChanged"].Value,
                "export-procedure-mismatch" => localizer["Home_ExportParamsChanged"].Value,
                "export-expired" => localizer["Home_ExportExpired"].Value,
                _ => localizer["Home_ExportRequiresData"].Value
            };

            return Content(
                $"""
                 <span class="text-sm text-red-700">{message}</span>
                 """,
                "text/html");
        }

        var procedure = await procedures.GetByIdAsync(id, cancellationToken);
        if (procedure is null || !procedure.Enabled)
        {
            exportEligibility.Clear(user.Username);
            return Content(
                $"""
                 <span class="text-sm text-red-700">{localizer["Home_ProcedureNotFound"]}</span>
                 """,
                "text/html");
        }

        var jobId = exports.QueueExport(id, parameters, user.Username);
        return PartialView("Partials/_ExportStatus", jobId);
    }

    [HttpGet("/Home/ExportStatus")]
    public IActionResult ExportStatus(Guid jobId)
        => PartialView("Partials/_ExportStatus", jobId);

    private int ResolveProcedureId(int? procedureId)
    {
        if (procedureId is > 0)
        {
            return procedureId.Value;
        }

        if (Request.HasFormContentType)
        {
            if (int.TryParse(Request.Form["procedureId"].FirstOrDefault(), out var fromForm) && fromForm > 0)
            {
                return fromForm;
            }

            if (int.TryParse(Request.Form["ProcedureId"].FirstOrDefault(), out var fromFormPascal) && fromFormPascal > 0)
            {
                return fromFormPascal;
            }
        }

        return 0;
    }

    private ExecutionResultDto CreateSelectionRequiredResult()
        => new()
        {
            Success = false,
            ErrorMessage = localizer["Home_SelectProcedureRequired"].Value,
            ProcedureId = 0,
            RowCount = 0,
            Data = null,
            Columns = []
        };

    private static ExecutionResultDto CreateErrorResult(int procedureId, string message)
        => new()
        {
            Success = false,
            ErrorMessage = message,
            ProcedureId = procedureId,
            RowCount = 0,
            Data = null,
            Columns = []
        };

    private Dictionary<string, string?> CollectParameters()
    {
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("param_", StringComparison.OrdinalIgnoreCase)))
        {
            var name = key["param_".Length..];
            if (ProcedurePagination.IsReservedParameterName(name))
            {
                continue;
            }

            var raw = Request.Form[key].ToString();
            dict[name] = string.IsNullOrEmpty(raw) ? raw : raw.Trim();
        }

        foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("paramcheck_", StringComparison.OrdinalIgnoreCase)))
        {
            var name = key["paramcheck_".Length..];
            if (ProcedurePagination.IsReservedParameterName(name))
            {
                continue;
            }

            if (!dict.ContainsKey(name))
            {
                dict[name] = "false";
            }
        }

        return dict;
    }

    private (long PageNumber, long PageSize) CollectPaging()
    {
        long? pageNumber = null;
        long? pageSize = null;

        if (long.TryParse(Request.Form["pageNumber"].FirstOrDefault(), out var pn))
        {
            pageNumber = pn;
        }

        if (long.TryParse(Request.Form["pageSize"].FirstOrDefault(), out var ps))
        {
            pageSize = ps;
        }

        return (
            ProcedurePagination.ClampPageNumber(pageNumber),
            ProcedurePagination.ClampUiPageSize(pageSize));
    }
}
