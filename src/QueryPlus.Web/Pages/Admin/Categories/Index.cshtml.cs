using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.Interfaces;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Web.Resources;

namespace QueryPlus.Web.Pages.Admin.Categories;

public class IndexModel : PageModel
{
    private readonly ICategoryService _categories;
    private readonly IStringLocalizer<SharedResource> _L;

    public IndexModel(ICategoryService categories, IStringLocalizer<SharedResource> localizer)
    {
        _categories = categories;
        _L = localizer;
    }

    [BindProperty(SupportsGet = true)]
    public string? Description { get; set; }

    public IReadOnlyList<CategoryListItemDto> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Items = await _categories.SearchAsync(new CategoryFilterDto { Description = Description }, cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _categories.DeleteAsync(id, cancellationToken);
            TempData["Success"] = _L["Categories_Deleted"].Value;
        }
        catch (Exception ex) when (ex is DomainException or Application.Common.ValidationException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }
}
