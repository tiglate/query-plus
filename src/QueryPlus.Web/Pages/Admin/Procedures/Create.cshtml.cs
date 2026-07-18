using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using QueryPlus.Application.Interfaces;
using QueryPlus.Web.Resources;
using QueryPlus.Web.ViewModels;
using AppValidationException = QueryPlus.Application.Common.ValidationException;

namespace QueryPlus.Web.Pages.Admin.Procedures;

public class CreateModel : ProcedureEditFormBase
{
    public CreateModel(
        IProcedureService procedures,
        ICategoryService categories,
        IProcedureMetadataSyncService metadataSync,
        IStringLocalizer<SharedResource> localizer)
        : base(procedures, categories, metadataSync, localizer)
    {
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Input = new ProcedureEditViewModel { Enabled = true, RoleEntitlement = string.Empty };
        await LoadLookupsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken)
    {
        await LoadLookupsAsync(cancellationToken);

        // Ensure nested view-model validation runs even if binder left empty strings.
        if (!TryValidateModel(Input, nameof(Input)))
        {
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var dto = ProcedureEditMapper.ToSaveDto(Input);
            var created = await Procedures.CreateAsync(dto, cancellationToken);
            TempData["Success"] = L["Procedures_Saved"].Value;
            return RedirectToPage("View", new { id = created.Id });
        }
        catch (AppValidationException ex)
        {
            AddValidationErrors(ex);
            return Page();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}
