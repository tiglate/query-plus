using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using QueryPlus.Application.Interfaces;
using QueryPlus.Web.ViewModels;

namespace QueryPlus.Web.Pages.Admin.Procedures;

public class ViewModel(IProcedureService procedures, ICategoryService categories) : PageModel
{
    public ProcedureEditViewModel Input { get; private set; } = new();
    public List<SelectListItem> CategoryOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await procedures.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        Input = ProcedureEditMapper.FromDetail(entity, readOnly: true);
        var cats = await categories.ListAllAsync(cancellationToken);
        CategoryOptions = cats
            .Select(c => new SelectListItem(c.Description, c.Id.ToString(), Input.CategoryId == c.Id))
            .ToList();
        return Page();
    }
}
