using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using QueryPlus.Application.Interfaces;
using QueryPlus.Domain.Enums;
using QueryPlus.Web.Resources;
using QueryPlus.Web.ViewModels;
using AppValidationException = QueryPlus.Application.Common.ValidationException;

namespace QueryPlus.Web.Pages.Admin.Procedures;

public abstract class ProcedureEditFormBase : PageModel
{
    protected readonly IProcedureService Procedures;
    protected readonly ICategoryService Categories;
    protected readonly IProcedureMetadataSyncService MetadataSync;
    protected readonly IStringLocalizer<SharedResource> L;

    protected ProcedureEditFormBase(
        IProcedureService procedures,
        ICategoryService categories,
        IProcedureMetadataSyncService metadataSync,
        IStringLocalizer<SharedResource> localizer)
    {
        Procedures = procedures;
        Categories = categories;
        MetadataSync = metadataSync;
        L = localizer;
    }

    [BindProperty]
    public ProcedureEditViewModel Input { get; set; } = new();

    public List<SelectListItem> CategoryOptions { get; protected set; } = [];

    protected async Task LoadLookupsAsync(CancellationToken cancellationToken)
    {
        var cats = await Categories.ListAllAsync(cancellationToken);
        CategoryOptions = cats
            .Select(c => new SelectListItem(c.Description, c.Id.ToString(), Input.CategoryId == c.Id))
            .ToList();

        // Placeholder so CategoryId stays 0 until the user explicitly chooses a category.
        CategoryOptions.Insert(0, new SelectListItem(L["Procedures_SelectCategory"].Value, "")
        {
            Selected = Input.CategoryId <= 0
        });
    }

    public async Task<IActionResult> OnPostAddParameterAsync(CancellationToken cancellationToken)
    {
        // Structural posts must not surface full-form validation (e.g. empty Caption while drafting).
        ModelState.Clear();
        Input.Parameters.Add(new ParameterEditViewModel
        {
            Caption = "Param",
            Name = "@Param",
            ParameterType = ParameterType.FreeText
        });
        await LoadLookupsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveParameterAsync(int index, CancellationToken cancellationToken)
    {
        ModelState.Clear();
        if (index >= 0 && index < Input.Parameters.Count)
        {
            Input.Parameters.RemoveAt(index);
        }

        await LoadLookupsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAddColumnAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        Input.Columns.Add(new ColumnEditViewModel
        {
            TechnicalName = "Column1",
            Caption = "Column1",
            Alignment = ColumnAlignment.Left,
            Visible = true
        });
        await LoadLookupsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveColumnAsync(int index, CancellationToken cancellationToken)
    {
        ModelState.Clear();
        if (index >= 0 && index < Input.Columns.Count)
        {
            Input.Columns.RemoveAt(index);
        }

        await LoadLookupsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostSyncMetadataAsync(CancellationToken cancellationToken)
    {
        // Keep only sync-related errors, not full save validation for incomplete drafts.
        ModelState.Clear();
        await LoadLookupsAsync(cancellationToken);

        var databaseName = Input.DatabaseName?.Trim() ?? string.Empty;
        var procedureName = Input.ProcedureName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(databaseName) || string.IsNullOrWhiteSpace(procedureName))
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                ModelState.AddModelError("Input.DatabaseName", L["Procedures_SyncMetadata_RequiresDatabase"].Value);
            }

            if (string.IsNullOrWhiteSpace(procedureName))
            {
                ModelState.AddModelError("Input.ProcedureName", L["Procedures_SyncMetadata_RequiresProcedure"].Value);
            }

            TempData["Error"] = L["Procedures_SyncMetadata_Hint"].Value;
            return Page();
        }

        try
        {
            var snapshot = await MetadataSync.FetchAsync(
                databaseName,
                procedureName,
                cancellationToken);
            ProcedureEditMapper.ApplySnapshot(Input, snapshot);
            TempData["Success"] = L["Procedures_SyncMetadata_Success"].Value;
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return Page();
    }

    protected void AddValidationErrors(AppValidationException ex)
    {
        foreach (var pair in ex.Errors)
        {
            // FluentValidation keys are relative to SaveProcedureDto; form ModelState uses "Input.*".
            var key = string.IsNullOrWhiteSpace(pair.Key)
                ? string.Empty
                : pair.Key.StartsWith("Input.", StringComparison.Ordinal)
                    ? pair.Key
                    : $"Input.{pair.Key}";

            foreach (var msg in pair.Value)
            {
                ModelState.AddModelError(key, msg);
            }
        }
    }
}
