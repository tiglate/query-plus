using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.DTOs.Common;

namespace QueryPlus.Application.Interfaces;

public interface ICategoryService
{
    Task<PagedResult<CategoryListItemDto>> SearchAsync(
        CategoryFilterDto filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// All categories ordered by description (dropdowns / lookups).
    /// </summary>
    Task<IReadOnlyList<CategoryListItemDto>> ListAllAsync(
        CancellationToken cancellationToken = default);

    Task<CategoryDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<CategoryDetailDto> CreateAsync(
        CreateCategoryDto dto,
        CancellationToken cancellationToken = default);

    Task<CategoryDetailDto> UpdateAsync(
        UpdateCategoryDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
