using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.Interfaces;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Web.Models;
using QueryPlus.Web.Resources;

namespace QueryPlus.Web.Pages.Admin.Categories;

public class IndexModel(ICategoryService categories, IStringLocalizer<SharedResource> localizer)
    : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Description { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = PagedResult<CategoryListItemDto>.DefaultPageSize;

    public PagedResult<CategoryListItemDto> Result { get; private set; } = new()
    {
        Items = [],
        TotalCount = 0,
        Page = 1,
        PageSize = PagedResult<CategoryListItemDto>.DefaultPageSize
    };

    public IReadOnlyList<CategoryListItemDto> Items => Result.Items;

    public PagerModel Pager { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Result = await categories.SearchAsync(new CategoryFilterDto
        {
            Description = Description,
            Page = PageNumber,
            PageSize = PageSize
        }, cancellationToken);

        // Keep bind properties in sync with normalized values returned by the service.
        PageNumber = Result.Page;
        PageSize = Result.PageSize;

        Pager = new PagerModel
        {
            Page = Result.Page,
            PageSize = Result.PageSize,
            TotalCount = Result.TotalCount,
            TotalPages = Result.TotalPages,
            PagePath = "./Index",
            RouteValues = BuildRouteValues()
        };
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await categories.DeleteAsync(id, cancellationToken);
            TempData["Success"] = localizer["Categories_Deleted"].Value;
        }
        catch (Exception ex) when (ex is DomainException or Application.Common.ValidationException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new
        {
            Description,
            pageNumber = PageNumber,
            pageSize = PageSize
        });
    }

    private Dictionary<string, string?> BuildRouteValues()
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(Description))
        {
            values["Description"] = Description;
        }

        if (PageSize != PagedResult<CategoryListItemDto>.DefaultPageSize)
        {
            values["pageSize"] = PageSize.ToString();
        }

        return values;
    }
}
