using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using QueryPlus.Application.Interfaces;
using QueryPlus.Web.Resources;
using QueryPlus.Web.ViewModels;
using AppValidationException = QueryPlus.Application.Common.ValidationException;

namespace QueryPlus.Web.Pages.Admin.Procedures;

public class EditModel : ProcedureEditFormBase
{
    public EditModel(
        IProcedureService procedures,
        ICategoryService categories,
        IProcedureMetadataSyncService metadataSync,
        IStringLocalizer<SharedResource> localizer)
        : base(procedures, categories, metadataSync, localizer)
    {
    }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await Procedures.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        Input = ProcedureEditMapper.FromDetail(entity);
        await LoadLookupsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken)
    {
        await LoadLookupsAsync(cancellationToken);

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
            var updated = await Procedures.UpdateAsync(dto, cancellationToken);
            TempData["Success"] = L["Procedures_Saved"].Value;
            return RedirectToPage("View", new { id = updated.Id });
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
