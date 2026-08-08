namespace QueryPlus.Application.DTOs.Categories;

public sealed class CategoryFilterDto
{
    public string? Description { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
