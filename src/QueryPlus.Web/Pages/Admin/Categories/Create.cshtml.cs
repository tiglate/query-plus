using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.Interfaces;
using QueryPlus.Web.Resources;
using AppValidationException = QueryPlus.Application.Common.ValidationException;

namespace QueryPlus.Web.Pages.Admin.Categories;

public class CreateModel : PageModel
{
    private readonly ICategoryService _categories;
    private readonly IStringLocalizer<SharedResource> _L;

    public CreateModel(ICategoryService categories, IStringLocalizer<SharedResource> localizer)
    {
        _categories = categories;
        _L = localizer;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, StringLength(200)]
        public string Description { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var created = await _categories.CreateAsync(
                new CreateCategoryDto { Description = Input.Description },
                cancellationToken);
            TempData["Success"] = _L["Categories_Saved"].Value;
            return RedirectToPage("View", new { id = created.Id });
        }
        catch (AppValidationException ex)
        {
            foreach (var pair in ex.Errors)
            {
                foreach (var msg in pair.Value)
                {
                    ModelState.AddModelError(pair.Key, msg);
                }
            }
            return Page();
        }
    }
}
