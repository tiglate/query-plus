namespace QueryPlus.Application.DTOs.Categories;

public sealed class CategoryListItemDto
{
    public int Id { get; init; }
    public required string Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
