using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.Interfaces;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Web.Models;
using QueryPlus.Web.Resources;
using QueryPlus.Web.ViewModels;
using AppValidationException = QueryPlus.Application.Common.ValidationException;

namespace QueryPlus.Web.Controllers.Admin;

[Route("Admin/Categories")]
public sealed class CategoriesController(
    ICategoryService categories,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    public sealed class CategoryInputModel
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Description { get; set; } = string.Empty;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(
        string? description,
        int pageNumber = 1,
        int pageSize = 0,
        CancellationToken cancellationToken = default)
    {
        ViewData["PageKey"] = "admin-categories";
        if (pageSize <= 0)
        {
            pageSize = PagedResult<CategoryListItemDto>.DefaultPageSize;
        }

        var result = await categories.SearchAsync(new CategoryFilterDto
        {
            Description = description,
            Page = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        var pager = new PagerModel
        {
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages,
            Controller = "Categories",
            Action = "Index",
            RouteValues = BuildRouteValues(description, result.PageSize)
        };

        return View(new CategoryIndexViewModel
        {
            Description = description,
            PageNumber = result.Page,
            PageSize = result.PageSize,
            Result = result,
            Pager = pager
        });
    }

    [HttpPost("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int id,
        string? description,
        int pageNumber = 1,
        int pageSize = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await categories.DeleteAsync(id, cancellationToken);
            TempData["Success"] = localizer["Categories_Deleted"].Value;
        }
        catch (Exception ex) when (ex is DomainException or AppValidationException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new
        {
            description,
            pageNumber,
            pageSize = pageSize > 0 ? pageSize : (int?)null
        });
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        ViewData["PageKey"] = "admin-categories";
        return View(new CategoryInputModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryInputModel input, CancellationToken cancellationToken = default)
    {
        ViewData["PageKey"] = "admin-categories";
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        try
        {
            var created = await categories.CreateAsync(
                new CreateCategoryDto { Description = input.Description },
                cancellationToken);
            TempData["Success"] = localizer["Categories_Saved"].Value;
            return RedirectToAction(nameof(Details), new { id = created.Id });
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

            return View(input);
        }
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        ViewData["PageKey"] = "admin-categories";
        var entity = await categories.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        ViewBag.CreatedAt = entity.CreatedAt;
        ViewBag.UpdatedAt = entity.UpdatedAt;
        return View(new CategoryInputModel { Id = entity.Id, Description = entity.Description });
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryInputModel input, CancellationToken cancellationToken = default)
    {
        ViewData["PageKey"] = "admin-categories";
        input.Id = id;
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        try
        {
            await categories.UpdateAsync(
                new UpdateCategoryDto { Id = input.Id, Description = input.Description },
                cancellationToken);
            TempData["Success"] = localizer["Categories_Saved"].Value;
            return RedirectToAction(nameof(Details), new { id = input.Id });
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

            return View(input);
        }
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        ViewData["PageKey"] = "admin-categories";
        var entity = await categories.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        return View(entity);
    }

    private static Dictionary<string, string?> BuildRouteValues(string? description, int pageSize)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(description))
        {
            values["description"] = description;
        }

        if (pageSize != PagedResult<CategoryListItemDto>.DefaultPageSize)
        {
            values["pageSize"] = pageSize.ToString();
        }

        return values;
    }
}
