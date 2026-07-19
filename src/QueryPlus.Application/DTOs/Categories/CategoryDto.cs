namespace QueryPlus.Application.DTOs.Categories;

public sealed class CategoryListItemDto
{
    public int Id { get; init; }
    public required string Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class CategoryDetailDto
{
    public int Id { get; init; }
    public required string Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class CategoryFilterDto
{
    public string? Description { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class CreateCategoryDto
{
    public required string Description { get; init; }
}

public sealed class UpdateCategoryDto
{
    public int Id { get; init; }
    public required string Description { get; init; }
}
