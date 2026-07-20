using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Interfaces;
using QueryPlus.Domain.Enums;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Web.Models;
using QueryPlus.Web.Resources;
using QueryPlus.Web.ViewModels;
using AppValidationException = QueryPlus.Application.Common.ValidationException;
// ViewModels used for Index list screens

namespace QueryPlus.Web.Controllers.Admin;

[Route("Admin/Procedures")]
public sealed class ProceduresController(
    IProcedureService procedures,
    ICategoryService categories,
    IProcedureMetadataSyncService metadataSync,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(
        int? categoryId,
        string? caption,
        string? roleEntitlement,
        bool? enabled,
        int pageNumber = 1,
        int pageSize = 0,
        CancellationToken cancellationToken = default)
    {
        ViewData["PageKey"] = "admin-procedures";
        if (pageSize <= 0)
        {
            pageSize = PagedResult<ProcedureListItemDto>.DefaultPageSize;
        }

        var categoryOptions = await BuildCategoryFilterOptionsAsync(categoryId, cancellationToken);

        var result = await procedures.SearchAsync(new ProcedureFilterDto
        {
            CategoryId = categoryId,
            Caption = caption,
            RoleEntitlement = roleEntitlement,
            Enabled = enabled,
            Page = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        return View(new ProcedureIndexViewModel
        {
            CategoryId = categoryId,
            Caption = caption,
            RoleEntitlement = roleEntitlement,
            Enabled = enabled,
            PageNumber = result.Page,
            PageSize = result.PageSize,
            Result = result,
            CategoryOptions = categoryOptions,
            Pager = new PagerModel
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages,
                Controller = "Procedures",
                Action = "Index",
                RouteValues = BuildRouteValues(categoryId, caption, roleEntitlement, enabled, result.PageSize)
            }
        });
    }

    [HttpPost("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int id,
        int? categoryId,
        string? caption,
        string? roleEntitlement,
        bool? enabled,
        int pageNumber = 1,
        int pageSize = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await procedures.DeleteAsync(id, cancellationToken);
            TempData["Success"] = localizer["Procedures_Deleted"].Value;
        }
        catch (Exception ex) when (ex is DomainException or AppValidationException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new
        {
            categoryId,
            caption,
            roleEntitlement,
            enabled,
            pageNumber,
            pageSize = pageSize > 0 ? pageSize : (int?)null
        });
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        ViewData["PageKey"] = "admin-procedure-edit";
        var input = new ProcedureEditViewModel { Enabled = true, RoleEntitlement = string.Empty };
        await LoadLookupsAsync(input, cancellationToken);
        return View(input);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Create(
        ProcedureEditViewModel input,
        CancellationToken cancellationToken = default)
        => SaveNewAsync(input, cancellationToken);

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        ViewData["PageKey"] = "admin-procedure-edit";
        var entity = await procedures.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        var input = ProcedureEditMapper.FromDetail(entity);
        await LoadLookupsAsync(input, cancellationToken);
        return View(input);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(
        int id,
        ProcedureEditViewModel input,
        CancellationToken cancellationToken = default)
        => SaveExistingAsync(id, input, cancellationToken);

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        ViewData["PageKey"] = "admin-procedure-edit";
        var entity = await procedures.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        var input = ProcedureEditMapper.FromDetail(entity, readOnly: true);
        var cats = await categories.ListAllAsync(cancellationToken);
        ViewBag.CategoryOptions = cats
            .Select(c => new SelectListItem(c.Description, c.Id.ToString(), input.CategoryId == c.Id))
            .ToList();
        return View(input);
    }

    // --- Structural form posts (Create/Edit) ---

    [HttpPost("Create/AddParameter")]
    [HttpPost("Edit/{id:int}/AddParameter")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddParameter(
        int? id,
        ProcedureEditViewModel input,
        CancellationToken cancellationToken = default)
    {
        ModelState.Clear();
        input.Parameters.Add(new ParameterEditViewModel
        {
            Caption = "Param",
            Name = "@Param",
            ParameterType = ParameterType.FreeText
        });
        return await RedisplayEditFormAsync(id, input, cancellationToken);
    }

    [HttpPost("Create/RemoveParameter")]
    [HttpPost("Edit/{id:int}/RemoveParameter")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveParameter(
        int? id,
        int index,
        ProcedureEditViewModel input,
        CancellationToken cancellationToken = default)
    {
        ModelState.Clear();
        if (index >= 0 && index < input.Parameters.Count)
        {
            input.Parameters.RemoveAt(index);
        }

        return await RedisplayEditFormAsync(id, input, cancellationToken);
    }

    [HttpPost("Create/AddColumn")]
    [HttpPost("Edit/{id:int}/AddColumn")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddColumn(
        int? id,
        ProcedureEditViewModel input,
        CancellationToken cancellationToken = default)
    {
        ModelState.Clear();
        input.Columns.Add(new ColumnEditViewModel
        {
            TechnicalName = "Column1",
            Caption = "Column1",
            Alignment = ColumnAlignment.Left,
            Visible = true
        });
        return await RedisplayEditFormAsync(id, input, cancellationToken);
    }

    [HttpPost("Create/RemoveColumn")]
    [HttpPost("Edit/{id:int}/RemoveColumn")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveColumn(
        int? id,
        int index,
        ProcedureEditViewModel input,
        CancellationToken cancellationToken = default)
    {
        ModelState.Clear();
        if (index >= 0 && index < input.Columns.Count)
        {
            input.Columns.RemoveAt(index);
        }

        return await RedisplayEditFormAsync(id, input, cancellationToken);
    }

    [HttpPost("Create/SyncMetadata")]
    [HttpPost("Edit/{id:int}/SyncMetadata")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncMetadata(
        int? id,
        ProcedureEditViewModel input,
        CancellationToken cancellationToken = default)
    {
        ModelState.Clear();
        await LoadLookupsAsync(input, cancellationToken);

        var databaseName = input.DatabaseName?.Trim() ?? string.Empty;
        var procedureName = input.ProcedureName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(databaseName) || string.IsNullOrWhiteSpace(procedureName))
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                ModelState.AddModelError("DatabaseName", localizer["Procedures_SyncMetadata_RequiresDatabase"].Value);
            }

            if (string.IsNullOrWhiteSpace(procedureName))
            {
                ModelState.AddModelError("ProcedureName", localizer["Procedures_SyncMetadata_RequiresProcedure"].Value);
            }

            TempData["Error"] = localizer["Procedures_SyncMetadata_Hint"].Value;
            return await RedisplayEditFormAsync(id, input, cancellationToken);
        }

        try
        {
            var snapshot = await metadataSync.FetchAsync(databaseName, procedureName, cancellationToken);
            ProcedureEditMapper.ApplySnapshot(input, snapshot);
            TempData["Success"] = localizer["Procedures_SyncMetadata_Success"].Value;
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return await RedisplayEditFormAsync(id, input, cancellationToken);
    }

    private async Task<IActionResult> SaveNewAsync(
        ProcedureEditViewModel input,
        CancellationToken cancellationToken = default)
    {
        ViewData["PageKey"] = "admin-procedure-edit";
        await LoadLookupsAsync(input, cancellationToken);

        if (!TryValidateModel(input) || !ModelState.IsValid)
        {
            return View("Create", input);
        }

        try
        {
            var dto = ProcedureEditMapper.ToSaveDto(input);
            var created = await procedures.CreateAsync(dto, cancellationToken);
            TempData["Success"] = localizer["Procedures_Saved"].Value;
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (AppValidationException ex)
        {
            AddValidationErrors(ex);
            return View("Create", input);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Create", input);
        }
    }

    private async Task<IActionResult> SaveExistingAsync(
        int id,
        ProcedureEditViewModel input,
        CancellationToken cancellationToken = default)
    {
        ViewData["PageKey"] = "admin-procedure-edit";
        input.Id = id;
        await LoadLookupsAsync(input, cancellationToken);

        if (!TryValidateModel(input) || !ModelState.IsValid)
        {
            return View("Edit", input);
        }

        try
        {
            var dto = ProcedureEditMapper.ToSaveDto(input);
            var updated = await procedures.UpdateAsync(dto, cancellationToken);
            TempData["Success"] = localizer["Procedures_Saved"].Value;
            return RedirectToAction(nameof(Details), new { id = updated.Id });
        }
        catch (AppValidationException ex)
        {
            AddValidationErrors(ex);
            return View("Edit", input);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Edit", input);
        }
    }

    private async Task<IActionResult> RedisplayEditFormAsync(
        int? id,
        ProcedureEditViewModel input,
        CancellationToken cancellationToken = default)
    {
        ViewData["PageKey"] = "admin-procedure-edit";
        if (id is > 0)
        {
            input.Id = id;
        }

        await LoadLookupsAsync(input, cancellationToken);
        return View(id is > 0 ? "Edit" : "Create", input);
    }

    private async Task LoadLookupsAsync(ProcedureEditViewModel input, CancellationToken cancellationToken = default)
    {
        var cats = await categories.ListAllAsync(cancellationToken);
        var options = cats
            .Select(c => new SelectListItem(c.Description, c.Id.ToString(), input.CategoryId == c.Id))
            .ToList();
        options.Insert(0, new SelectListItem(localizer["Procedures_SelectCategory"].Value, "")
        {
            Selected = input.CategoryId <= 0
        });
        ViewBag.CategoryOptions = options;
    }

    private async Task<List<SelectListItem>> BuildCategoryFilterOptionsAsync(
        int? categoryId,
        CancellationToken cancellationToken = default)
    {
        var cats = await categories.ListAllAsync(cancellationToken);
        var options = cats
            .Select(c => new SelectListItem(c.Description, c.Id.ToString(), categoryId == c.Id))
            .ToList();
        options.Insert(0, new SelectListItem("—", ""));
        return options;
    }

    private void AddValidationErrors(AppValidationException ex)
    {
        foreach (var pair in ex.Errors)
        {
            var key = string.IsNullOrWhiteSpace(pair.Key)
                ? string.Empty
                : pair.Key.StartsWith("Input.", StringComparison.Ordinal)
                    ? pair.Key["Input.".Length..]
                    : pair.Key;

            foreach (var msg in pair.Value)
            {
                ModelState.AddModelError(key, msg);
            }
        }
    }

    private static Dictionary<string, string?> BuildRouteValues(
        int? categoryId,
        string? caption,
        string? roleEntitlement,
        bool? enabled,
        int pageSize)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (categoryId is not null)
        {
            values["categoryId"] = categoryId.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(caption))
        {
            values["caption"] = caption;
        }

        if (!string.IsNullOrWhiteSpace(roleEntitlement))
        {
            values["roleEntitlement"] = roleEntitlement;
        }

        if (enabled is not null)
        {
            values["enabled"] = enabled.Value ? "true" : "false";
        }

        if (pageSize != PagedResult<ProcedureListItemDto>.DefaultPageSize)
        {
            values["pageSize"] = pageSize.ToString();
        }

        return values;
    }
}
