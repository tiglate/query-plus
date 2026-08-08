using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Application.Mapping;

/// <summary>
/// Entity → DTO projections for <see cref="Category"/>.
/// </summary>
public static class CategoryMapper
{
    public static CategoryListItemDto ToListItemDto(Category entity) => new()
    {
        Id = entity.IdCategory,
        Description = entity.Description,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };

    public static IReadOnlyList<CategoryListItemDto> ToListItemDtos(IEnumerable<Category> entities) =>
        entities.Select(ToListItemDto).ToArray();

    public static CategoryDetailDto ToDetailDto(Category entity) => new()
    {
        Id = entity.IdCategory,
        Description = entity.Description,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };

    public static IReadOnlyList<CategoryDetailDto> ToDetailDtos(IEnumerable<Category> entities) =>
        entities.Select(ToDetailDto).ToArray();
}
