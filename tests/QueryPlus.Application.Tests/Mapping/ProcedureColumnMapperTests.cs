using FluentAssertions;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Mapping;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.Tests;

public class ProcedureColumnMapperTests
{
    [Fact]
    public void ToDto_maps_all_fields()
    {
        var entity = new ProcedureColumn
        {
            IdProcedureColumn = 3,
            TechnicalName = "Amount",
            Caption = "Amount",
            Alignment = ColumnAlignment.Right,
            FormatMask = "C2",
            Visible = false
        };

        var dto = ProcedureColumnMapper.ToDto(entity);

        dto.Id.Should().Be(3);
        dto.TechnicalName.Should().Be("Amount");
        dto.Caption.Should().Be("Amount");
        dto.Alignment.Should().Be(ColumnAlignment.Right);
        dto.FormatMask.Should().Be("C2");
        dto.Visible.Should().BeFalse();
    }

    [Fact]
    public void ToGridColumnDto_omits_id_and_keeps_visible_shape()
    {
        var entity = new ProcedureColumn
        {
            IdProcedureColumn = 3,
            TechnicalName = "Amount",
            Caption = "Amount",
            Alignment = ColumnAlignment.Right,
            FormatMask = "C2",
            Visible = true
        };

        var dto = ProcedureColumnMapper.ToGridColumnDto(entity);

        dto.Should().BeOfType<GridColumnDto>();
        dto.TechnicalName.Should().Be("Amount");
        dto.Caption.Should().Be("Amount");
        dto.Alignment.Should().Be(ColumnAlignment.Right);
        dto.FormatMask.Should().Be("C2");
        dto.Visible.Should().BeTrue();
    }

    [Fact]
    public void Collection_overloads_materialize_arrays()
    {
        var entities = new[]
        {
            new ProcedureColumn { IdProcedureColumn = 1, TechnicalName = "a", Caption = "A" },
            new ProcedureColumn { IdProcedureColumn = 2, TechnicalName = "b", Caption = "B" }
        };

        ProcedureColumnMapper.ToDtos(entities).Should().HaveCount(2);
        ProcedureColumnMapper.ToGridColumnDtos(entities).Should().HaveCount(2);
    }
}
