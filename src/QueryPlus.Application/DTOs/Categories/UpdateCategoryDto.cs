namespace QueryPlus.Application.DTOs.Categories;

public sealed class UpdateCategoryDto
{
    public int Id { get; init; }
    public required string Description { get; init; }
}
