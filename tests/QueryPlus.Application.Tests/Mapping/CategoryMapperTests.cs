using FluentAssertions;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.Mapping;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Application.Tests;

public class CategoryMapperTests
{
    [Fact]
    public void ToListItemDto_maps_all_fields()
    {
        var entity = new Category
        {
            IdCategory = 7,
            Description = "Sales",
            CreatedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 3, 4, 5, 6, DateTimeKind.Utc),
        };

        var dto = CategoryMapper.ToListItemDto(entity);

        dto.Should().BeOfType<CategoryListItemDto>();
        dto.Id.Should().Be(7);
        dto.Description.Should().Be("Sales");
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.UpdatedAt.Should().Be(entity.UpdatedAt);
    }

    [Fact]
    public void ToDetailDto_maps_all_fields()
    {
        var entity = new Category
        {
            IdCategory = 7,
            Description = "Sales",
            CreatedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
        };

        var dto = CategoryMapper.ToDetailDto(entity);

        dto.Id.Should().Be(7);
        dto.Description.Should().Be("Sales");
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.UpdatedAt.Should().BeNull();
        dto.CreatedBy.Should().BeNull();
        dto.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public void ToListItemDtos_materializes_collection()
    {
        var entities = new[]
        {
            new Category { IdCategory = 1, Description = "A" },
            new Category { IdCategory = 2, Description = "B" }
        };

        var dtos = CategoryMapper.ToListItemDtos(entities);

        dtos.Should().HaveCount(2);
        dtos[0].Id.Should().Be(1);
        dtos[1].Id.Should().Be(2);
    }
}
