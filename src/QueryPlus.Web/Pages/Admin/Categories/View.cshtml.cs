using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.Interfaces;

namespace QueryPlus.Web.Pages.Admin.Categories;

public class ViewModel(ICategoryService categories) : PageModel
{
    public CategoryDetailDto Item { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await categories.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        Item = entity;
        return Page();
    }
}
