using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

namespace QueryPlus.Web.Pages;

public class IndexModel(
    IProcedureService procedures,
    IProcedureRepository procedureRepository,
    IExecutionService execution,
    IExcelExportService exports,
    ExportEligibilityService exportEligibility,
    ICurrentUserContext user,
    IStringLocalizer<SharedResource> localizer)
    : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? ProcedureId { get; set; }

    public IReadOnlyList<ProcedureLookupDto> AccessibleProcedures { get; private set; } = [];
    public ProcedureDetailDto? SelectedProcedure { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["PageKey"] = "home";
        await LoadProceduresAsync(cancellationToken);
        if (ProcedureId is > 0)
        {
            SelectedProcedure = await procedures.GetByIdAsync(ProcedureId.Value, cancellationToken);
            // Drop selection if the procedure is no longer accessible / does not exist.
            if (SelectedProcedure is null || AccessibleProcedures.All(p => p.Id != ProcedureId))
            {
                ProcedureId = null;
                SelectedProcedure = null;
            }
        }
    }

    /// <summary>
    /// Accidental full form POST (e.g. Enter in a parameter field) must not render
    /// an empty page without reloading procedures. Redirect to a clean GET instead.
    /// </summary>
    public IActionResult OnPost()
    {
        ViewData["PageKey"] = "home";
        var procedureId = ResolveProcedureId(null);
        if (procedureId > 0)
        {
            return RedirectToPage(new { procedureId });
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetParametersAsync(int? procedureId, CancellationToken cancellationToken)
    {
        // Changing procedure invalidates any prior export eligibility.
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

        return Partial("Shared/Partials/_ParameterForm", procedure);
    }

    public async Task<IActionResult> OnPostExecuteAsync(int? procedureId, CancellationToken cancellationToken)
    {
        var id = ResolveProcedureId(procedureId);
        if (id <= 0)
        {
            exportEligibility.Clear(user.Username);
            return Partial("Shared/Partials/_ResultsGrid", CreateSelectionRequiredResult());
        }

        try
        {
            var parameters = CollectParameters();

            // Server-side required-parameter validation before execution.
            var procedureEntity = await procedureRepository.GetEnabledByIdWithDetailsAsync(id, cancellationToken);
            if (procedureEntity is null)
            {
                exportEligibility.Clear(user.Username);
                return Partial("Shared/Partials/_ResultsGrid",
                    CreateErrorResult(id, localizer["Home_ProcedureNotFound"].Value));
            }

            var missing = ParameterValueBinder.GetMissingRequiredCaptions(procedureEntity.Parameters, parameters);
            if (missing.Count > 0)
            {
                exportEligibility.Clear(user.Username);
                var message = string.Format(
                    localizer["Home_RequiredParametersMissing"].Value,
                    string.Join(", ", missing));
                return Partial("Shared/Partials/_ResultsGrid", CreateErrorResult(id, message));
            }

            var (pageNumber, pageSize) = CollectPaging();
            var result = await execution.ExecuteAsync(new ExecuteProcedureRequest
            {
                ProcedureId = id,
                ParameterValues = parameters,
                PageNumber = pageNumber,
                PageSize = pageSize
            }, cancellationToken);

            // Export eligibility uses total rows for paginated SPs so paging does not block export.
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

            return Partial("Shared/Partials/_ResultsGrid", result);
        }
        catch (ValidationException ex)
        {
            exportEligibility.Clear(user.Username);
            var message = string.Join(" ", ex.Errors.SelectMany(e => e.Value));
            return Partial("Shared/Partials/_ResultsGrid", CreateErrorResult(id, message));
        }
        catch (EntityNotFoundException)
        {
            exportEligibility.Clear(user.Username);
            return Partial("Shared/Partials/_ResultsGrid", CreateErrorResult(id, localizer["Home_ProcedureNotFound"].Value));
        }
        catch (ForbiddenOperationException ex)
        {
            exportEligibility.Clear(user.Username);
            return Partial("Shared/Partials/_ResultsGrid", CreateErrorResult(id, ex.Message));
        }
        catch (Exception)
        {
            exportEligibility.Clear(user.Username);
            return Partial(
                "Shared/Partials/_ResultsGrid",
                CreateErrorResult(id, localizer["Home_ExecuteFailed"].Value));
        }
    }

    public async Task<IActionResult> OnPostExportAsync(int? procedureId, CancellationToken cancellationToken)
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
        return Partial("Shared/Partials/_ExportStatus", jobId);
    }

    public IActionResult OnGetExportStatus(Guid jobId)
    {
        return Partial("Shared/Partials/_ExportStatus", jobId);
    }

    private int ResolveProcedureId(int? procedureId)
    {
        if (procedureId is > 0)
        {
            return procedureId.Value;
        }

        if (ProcedureId is > 0)
        {
            return ProcedureId.Value;
        }

        if (int.TryParse(Request.Form["procedureId"].FirstOrDefault(), out var fromForm) && fromForm > 0)
        {
            return fromForm;
        }

        if (int.TryParse(Request.Form["ProcedureId"].FirstOrDefault(), out var fromFormPascal) && fromFormPascal > 0)
        {
            return fromFormPascal;
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

    private async Task LoadProceduresAsync(CancellationToken cancellationToken)
    {
        AccessibleProcedures = await procedures.GetAccessibleForCurrentUserAsync(cancellationToken);
    }

    private Dictionary<string, string?> CollectParameters()
    {
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("param_", StringComparison.OrdinalIgnoreCase)))
        {
            var name = key["param_".Length..];
            // Never accept reserved pagination names from the form as user parameters.
            if (ProcedurePagination.IsReservedParameterName(name))
            {
                continue;
            }

            // HTMX / form posts bypass model binding for these keys — trim explicitly.
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

    /// <summary>
    /// Reads server-side paging controls from the form (hidden fields maintained by the results pager).
    /// </summary>
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
