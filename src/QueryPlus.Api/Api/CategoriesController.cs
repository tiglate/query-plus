using Microsoft.AspNetCore.Mvc;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.Interfaces;

namespace QueryPlus.Api.Api;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryService categories) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<CategoryListItemDto>> Search(string? description, int pageNumber = 1,
        int pageSize = PagedResult<CategoryListItemDto>.DefaultPageSize,
        CancellationToken cancellationToken = default) => categories.SearchAsync(
        new() { Description = description, Page = pageNumber, PageSize = pageSize }, cancellationToken);

    [HttpGet("lookup")]
    public Task<IReadOnlyList<CategoryListItemDto>> Lookup(CancellationToken cancellationToken) =>
        categories.ListAllAsync(cancellationToken);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDetailDto>> Get(int id, CancellationToken cancellationToken)
    {
        var result = await categories.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound(Problem(title: "Category not found", statusCode: 404)) : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDetailDto>> Create(CreateCategoryDto request,
        CancellationToken cancellationToken)
    {
        var result = await categories.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryDetailDto>> Update(int id, UpdateCategoryDto request,
        CancellationToken cancellationToken)
    {
        var existing = await categories.GetByIdAsync(id, cancellationToken);
        if (existing is null) return NotFound(Problem(title: "Category not found", statusCode: 404));
        return Ok(await categories.UpdateAsync(new() { Id = id, Description = request.Description },
            cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (await categories.GetByIdAsync(id, cancellationToken) is null)
            return NotFound(Problem(title: "Category not found", statusCode: 404));
        await categories.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
