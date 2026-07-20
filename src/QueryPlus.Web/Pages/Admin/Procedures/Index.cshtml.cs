using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Interfaces;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Web.Models;
using QueryPlus.Web.Resources;

namespace QueryPlus.Web.Pages.Admin.Procedures;

public class IndexModel(
    IProcedureService procedures,
    ICategoryService categories,
    IStringLocalizer<SharedResource> localizer)
    : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Caption { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? RoleEntitlement { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? Enabled { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = PagedResult<ProcedureListItemDto>.DefaultPageSize;

    public PagedResult<ProcedureListItemDto> Result { get; private set; } = new()
    {
        Items = [],
        TotalCount = 0,
        Page = 1,
        PageSize = PagedResult<ProcedureListItemDto>.DefaultPageSize
    };

    public IReadOnlyList<ProcedureListItemDto> Items => Result.Items;
    public List<SelectListItem> CategoryOptions { get; private set; } = [];
    public PagerModel Pager { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadCategoriesAsync(cancellationToken);
        Result = await procedures.SearchAsync(new ProcedureFilterDto
        {
            CategoryId = CategoryId,
            Caption = Caption,
            RoleEntitlement = RoleEntitlement,
            Enabled = Enabled,
            Page = PageNumber,
            PageSize = PageSize
        }, cancellationToken);

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
            await procedures.DeleteAsync(id, cancellationToken);
            TempData["Success"] = localizer["Procedures_Deleted"].Value;
        }
        catch (Exception ex) when (ex is DomainException or Application.Common.ValidationException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new
        {
            CategoryId,
            Caption,
            RoleEntitlement,
            Enabled,
            pageNumber = PageNumber,
            pageSize = PageSize
        });
    }

    private async Task LoadCategoriesAsync(CancellationToken cancellationToken)
    {
        var cats = await categories.ListAllAsync(cancellationToken);
        CategoryOptions = cats
            .Select(c => new SelectListItem(c.Description, c.Id.ToString(), CategoryId == c.Id))
            .ToList();
        CategoryOptions.Insert(0, new SelectListItem("—", ""));
    }

    private Dictionary<string, string?> BuildRouteValues()
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (CategoryId is not null)
        {
            values["CategoryId"] = CategoryId.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(Caption))
        {
            values["Caption"] = Caption;
        }

        if (!string.IsNullOrWhiteSpace(RoleEntitlement))
        {
            values["RoleEntitlement"] = RoleEntitlement;
        }

        if (Enabled is not null)
        {
            values["Enabled"] = Enabled.Value ? "true" : "false";
        }

        if (PageSize != PagedResult<ProcedureListItemDto>.DefaultPageSize)
        {
            values["pageSize"] = PageSize.ToString();
        }

        return values;
    }
}
