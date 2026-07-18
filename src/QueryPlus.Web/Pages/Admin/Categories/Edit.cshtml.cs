using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.Interfaces;
using QueryPlus.Web.Resources;
using AppValidationException = QueryPlus.Application.Common.ValidationException;

namespace QueryPlus.Web.Pages.Admin.Categories;

public class EditModel : PageModel
{
    private readonly ICategoryService _categories;
    private readonly IStringLocalizer<SharedResource> _L;

    public EditModel(ICategoryService categories, IStringLocalizer<SharedResource> localizer)
    {
        _categories = categories;
        _L = localizer;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public class InputModel
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Description { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _categories.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        Input = new InputModel { Id = entity.Id, Description = entity.Description };
        CreatedAt = entity.CreatedAt;
        UpdatedAt = entity.UpdatedAt;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _categories.UpdateAsync(
                new UpdateCategoryDto { Id = Input.Id, Description = Input.Description },
                cancellationToken);
            TempData["Success"] = _L["Categories_Saved"].Value;
            return RedirectToPage("View", new { id = Input.Id });
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
