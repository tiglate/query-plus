using FluentAssertions;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Mapping;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.Tests;

public class ProcedureMapperTests
{
    private static Procedure BuildProcedure(bool withCategory = true) => new()
    {
        IdProcedure = 10,
        IdCategory = 1,
        Caption = "Sales Report",
        DatabaseName = "Sales",
        ProcedureName = "dbo.usp_Sales",
        Enabled = true,
        SupportsPagination = true,
        RoleEntitlement = "user",
        Description = "Quarterly numbers",
        CreatedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 1, 3, 4, 5, 6, DateTimeKind.Utc),
        Category = withCategory
            ? new Category { IdCategory = 1, Description = "Sales" }
            : null!,
    };

    [Fact]
    public void ToListItemDto_maps_all_fields_and_includes_category_description()
    {
        var dto = ProcedureMapper.ToListItemDto(BuildProcedure());

        dto.Id.Should().Be(10);
        dto.CategoryId.Should().Be(1);
        dto.CategoryDescription.Should().Be("Sales");
        dto.Caption.Should().Be("Sales Report");
        dto.DatabaseName.Should().Be("Sales");
        dto.ProcedureName.Should().Be("dbo.usp_Sales");
        dto.Enabled.Should().BeTrue();
        dto.SupportsPagination.Should().BeTrue();
        dto.RoleEntitlement.Should().Be("user");
        dto.CreatedAt.Should().NotBe(default);
        dto.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ToListItemDto_returns_null_category_description_when_nav_is_null()
    {
        var dto = ProcedureMapper.ToListItemDto(BuildProcedure(withCategory: false));

        dto.CategoryDescription.Should().BeNull();
    }

    [Fact]
    public void ToLookupDto_maps_summary_shape()
    {
        var dto = ProcedureMapper.ToLookupDto(BuildProcedure());

        dto.Id.Should().Be(10);
        dto.CategoryId.Should().Be(1);
        dto.CategoryDescription.Should().Be("Sales");
        dto.Caption.Should().Be("Sales Report");
        dto.Description.Should().Be("Quarterly numbers");
        dto.RoleEntitlement.Should().Be("user");
        dto.SupportsPagination.Should().BeTrue();
    }

    [Fact]
    public void ToListItemDtos_and_ToLookupDtos_materialize_collections()
    {
        var entities = new[] { BuildProcedure(), BuildProcedure() };

        ProcedureMapper.ToListItemDtos(entities).Should().HaveCount(2);
        ProcedureMapper.ToLookupDtos(entities).Should().HaveCount(2);
    }

    [Fact]
    public void ToDetailDto_orders_columns_and_parameters_by_caption()
    {
        var entity = BuildProcedure();
        entity.Parameters =
        [
            new ProcedureParameter
            {
                IdProcedureParameter = 1,
                Caption = "Z-End",
                Name = "@End",
                ParameterType = ParameterType.Date
            },
            new ProcedureParameter
            {
                IdProcedureParameter = 2,
                Caption = "A-Start",
                Name = "@Start",
                ParameterType = ParameterType.Date
            }
        ];
        entity.Columns =
        [
            new ProcedureColumn
            {
                IdProcedureColumn = 1,
                TechnicalName = "b",
                Caption = "Beta"
            },
            new ProcedureColumn
            {
                IdProcedureColumn = 2,
                TechnicalName = "a",
                Caption = "Alpha"
            }
        ];

        var dto = ProcedureMapper.ToDetailDto(entity);

        dto.Parameters.Select(p => p.Caption).Should().Equal("A-Start", "Z-End");
        dto.Columns.Select(c => c.Caption).Should().Equal("Alpha", "Beta");
    }

    [Fact]
    public void ToDetailDto_strips_pagination_reserved_parameters()
    {
        var entity = BuildProcedure();
        entity.Parameters =
        [
            new ProcedureParameter { IdProcedureParameter = 1, Caption = "User", Name = "@User" },
            new ProcedureParameter { IdProcedureParameter = 2, Caption = "PageNum", Name = "@PageNumber" },
            new ProcedureParameter { IdProcedureParameter = 3, Caption = "PageSize", Name = "@PageSize" },
            new ProcedureParameter { IdProcedureParameter = 4, Caption = "Total", Name = "@TotalRecords" }
        ];

        var dto = ProcedureMapper.ToDetailDto(entity);

        dto.Parameters.Select(p => p.Name).Should().Equal("@User");
    }
}
