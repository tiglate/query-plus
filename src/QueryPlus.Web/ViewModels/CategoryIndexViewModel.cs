using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Web.Models;

namespace QueryPlus.Web.ViewModels;

public sealed class CategoryIndexViewModel
{
    public string? Description { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = PagedResult<CategoryListItemDto>.DefaultPageSize;
    public required PagedResult<CategoryListItemDto> Result { get; init; }
    public IReadOnlyList<CategoryListItemDto> Items => Result.Items;
    public required PagerModel Pager { get; init; }
}
