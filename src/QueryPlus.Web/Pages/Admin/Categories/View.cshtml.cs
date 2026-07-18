using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.Interfaces;

namespace QueryPlus.Web.Pages.Admin.Categories;

public class ViewModel : PageModel
{
    private readonly ICategoryService _categories;

    public ViewModel(ICategoryService categories)
    {
        _categories = categories;
    }

    public CategoryDetailDto Item { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _categories.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        Item = entity;
        return Page();
    }
}
