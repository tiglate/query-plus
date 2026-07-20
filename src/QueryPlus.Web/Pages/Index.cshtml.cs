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

public class IndexModel : PageModel
{
    private readonly IProcedureService _procedures;
    private readonly IProcedureRepository _procedureRepository;
    private readonly IExecutionService _execution;
    private readonly IExcelExportService _exports;
    private readonly ExportEligibilityService _exportEligibility;
    private readonly ICurrentUserContext _user;
    private readonly IStringLocalizer<SharedResource> _L;

    public IndexModel(
        IProcedureService procedures,
        IProcedureRepository procedureRepository,
        IExecutionService execution,
        IExcelExportService exports,
        ExportEligibilityService exportEligibility,
        ICurrentUserContext user,
        IStringLocalizer<SharedResource> localizer)
    {
        _procedures = procedures;
        _procedureRepository = procedureRepository;
        _execution = execution;
        _exports = exports;
        _exportEligibility = exportEligibility;
        _user = user;
        _L = localizer;
    }

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
            SelectedProcedure = await _procedures.GetByIdAsync(ProcedureId.Value, cancellationToken);
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
        _exportEligibility.Clear(_user.Username);

        var id = procedureId.GetValueOrDefault();
        if (id <= 0)
        {
            return Content(
                $"""
                 <p class="text-sm text-slate-500">{_L["Home_NoProcedure"]}</p>
                 """,
                "text/html");
        }

        var procedure = await _procedures.GetByIdAsync(id, cancellationToken);
        if (procedure is null)
        {
            return Content(
                $"""
                 <p class="text-sm text-slate-500">{_L["Home_NoProcedure"]}</p>
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
            _exportEligibility.Clear(_user.Username);
            return Partial("Shared/Partials/_ResultsGrid", CreateSelectionRequiredResult());
        }

        try
        {
            var parameters = CollectParameters();

            // Server-side required-parameter validation before execution.
            var procedureEntity = await _procedureRepository.GetEnabledByIdWithDetailsAsync(id, cancellationToken);
            if (procedureEntity is null)
            {
                _exportEligibility.Clear(_user.Username);
                return Partial("Shared/Partials/_ResultsGrid",
                    CreateErrorResult(id, _L["Home_ProcedureNotFound"].Value));
            }

            var missing = ParameterValueBinder.GetMissingRequiredCaptions(procedureEntity.Parameters, parameters);
            if (missing.Count > 0)
            {
                _exportEligibility.Clear(_user.Username);
                var message = string.Format(
                    _L["Home_RequiredParametersMissing"].Value,
                    string.Join(", ", missing));
                return Partial("Shared/Partials/_ResultsGrid", CreateErrorResult(id, message));
            }

            var result = await _execution.ExecuteAsync(new ExecuteProcedureRequest
            {
                ProcedureId = id,
                ParameterValues = parameters
            }, cancellationToken);

            if (result.Success && result.RowCount > 0 && result.Data is { Rows.Count: > 0 })
            {
                _exportEligibility.MarkEligible(_user.Username, id, parameters, result.RowCount);
            }
            else
            {
                _exportEligibility.Clear(_user.Username);
            }

            return Partial("Shared/Partials/_ResultsGrid", result);
        }
        catch (ValidationException ex)
        {
            _exportEligibility.Clear(_user.Username);
            var message = string.Join(" ", ex.Errors.SelectMany(e => e.Value));
            return Partial("Shared/Partials/_ResultsGrid", CreateErrorResult(id, message));
        }
        catch (EntityNotFoundException)
        {
            _exportEligibility.Clear(_user.Username);
            return Partial("Shared/Partials/_ResultsGrid", CreateErrorResult(id, _L["Home_ProcedureNotFound"].Value));
        }
        catch (ForbiddenOperationException ex)
        {
            _exportEligibility.Clear(_user.Username);
            return Partial("Shared/Partials/_ResultsGrid", CreateErrorResult(id, ex.Message));
        }
        catch (Exception)
        {
            _exportEligibility.Clear(_user.Username);
            return Partial(
                "Shared/Partials/_ResultsGrid",
                CreateErrorResult(id, _L["Home_ExecuteFailed"].Value));
        }
    }

    public async Task<IActionResult> OnPostExportAsync(int? procedureId, CancellationToken cancellationToken)
    {
        var id = ResolveProcedureId(procedureId);
        if (id <= 0)
        {
            return Content(
                $"""
                 <span class="text-sm text-red-700">{_L["Home_SelectProcedureRequired"]}</span>
                 """,
                "text/html");
        }

        var parameters = CollectParameters();
        if (!_exportEligibility.TryValidate(_user.Username, id, parameters, out var reason))
        {
            var message = reason switch
            {
                "export-no-rows" => _L["Home_ExportRequiresData"].Value,
                "export-params-mismatch" => _L["Home_ExportParamsChanged"].Value,
                "export-procedure-mismatch" => _L["Home_ExportParamsChanged"].Value,
                "export-expired" => _L["Home_ExportExpired"].Value,
                _ => _L["Home_ExportRequiresData"].Value
            };

            return Content(
                $"""
                 <span class="text-sm text-red-700">{message}</span>
                 """,
                "text/html");
        }

        var procedure = await _procedures.GetByIdAsync(id, cancellationToken);
        if (procedure is null || !procedure.Enabled)
        {
            _exportEligibility.Clear(_user.Username);
            return Content(
                $"""
                 <span class="text-sm text-red-700">{_L["Home_ProcedureNotFound"]}</span>
                 """,
                "text/html");
        }

        var jobId = _exports.QueueExport(id, parameters, _user.Username);
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
            ErrorMessage = _L["Home_SelectProcedureRequired"].Value,
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
        AccessibleProcedures = await _procedures.GetAccessibleForCurrentUserAsync(cancellationToken);
    }

    private Dictionary<string, string?> CollectParameters()
    {
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("param_", StringComparison.OrdinalIgnoreCase)))
        {
            var name = key["param_".Length..];
            // HTMX / form posts bypass model binding for these keys — trim explicitly.
            var raw = Request.Form[key].ToString();
            dict[name] = string.IsNullOrEmpty(raw) ? raw : raw.Trim();
        }

        foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("paramcheck_", StringComparison.OrdinalIgnoreCase)))
        {
            var name = key["paramcheck_".Length..];
            if (!dict.ContainsKey(name))
            {
                dict[name] = "false";
            }
        }

        return dict;
    }
}
