using QueryPlus.Application.DTOs.Categories;

namespace QueryPlus.Application.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryListItemDto>> SearchAsync(
        CategoryFilterDto filter,
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
