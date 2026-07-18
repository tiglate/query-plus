using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Interfaces;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Web.Resources;

namespace QueryPlus.Web.Pages.Admin.Procedures;

public class IndexModel : PageModel
{
    private readonly IProcedureService _procedures;
    private readonly ICategoryService _categories;
    private readonly IStringLocalizer<SharedResource> _L;

    public IndexModel(
        IProcedureService procedures,
        ICategoryService categories,
        IStringLocalizer<SharedResource> localizer)
    {
        _procedures = procedures;
        _categories = categories;
        _L = localizer;
    }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Caption { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? RoleEntitlement { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? Enabled { get; set; }

    public IReadOnlyList<ProcedureListItemDto> Items { get; private set; } = [];
    public List<SelectListItem> CategoryOptions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadCategoriesAsync(cancellationToken);
        Items = await _procedures.SearchAsync(new ProcedureFilterDto
        {
            CategoryId = CategoryId,
            Caption = Caption,
            RoleEntitlement = RoleEntitlement,
            Enabled = Enabled
        }, cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _procedures.DeleteAsync(id, cancellationToken);
            TempData["Success"] = _L["Procedures_Deleted"].Value;
        }
        catch (Exception ex) when (ex is DomainException or Application.Common.ValidationException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }

    private async Task LoadCategoriesAsync(CancellationToken cancellationToken)
    {
        var cats = await _categories.SearchAsync(new CategoryFilterDto(), cancellationToken);
        CategoryOptions = cats
            .Select(c => new SelectListItem(c.Description, c.Id.ToString(), CategoryId == c.Id))
            .ToList();
        CategoryOptions.Insert(0, new SelectListItem("—", ""));
    }
}
