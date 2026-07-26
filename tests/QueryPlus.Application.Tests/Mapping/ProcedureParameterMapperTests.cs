using FluentAssertions;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Mapping;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.Tests;

public class ProcedureParameterMapperTests
{
    [Fact]
    public void ToDto_maps_raw_fields()
    {
        var entity = new ProcedureParameter
        {
            IdProcedureParameter = 5,
            Caption = "Status",
            Name = "@Status",
            ParameterType = ParameterType.Combo,
            DefaultValue = "Active",
            ComboValues = "[\"Active\",\"Inactive\"]",
            IsRequired = true
        };

        var dto = ProcedureParameterMapper.ToDto(entity);

        dto.Id.Should().Be(5);
        dto.Caption.Should().Be("Status");
        dto.Name.Should().Be("@Status");
        dto.ParameterType.Should().Be(ParameterType.Combo);
        dto.DefaultValue.Should().Be("Active");
        dto.ComboValues.Should().Be("[\"Active\",\"Inactive\"]");
        dto.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void ToDtos_materializes_collection()
    {
        var entities = new[]
        {
            new ProcedureParameter { IdProcedureParameter = 1, Caption = "A", Name = "@A" },
            new ProcedureParameter { IdProcedureParameter = 2, Caption = "B", Name = "@B" }
        };

        ProcedureParameterMapper.ToDtos(entities).Should().HaveCount(2);
    }

    [Fact]
    public void ComboOptions_is_derived_from_combo_values_json()
    {
        var dto = new ProcedureParameterDto
        {
            Id = 1,
            Caption = "Status",
            Name = "@Status",
            ComboValues = "[\"Active\",\"Inactive\",\"Pending\"]"
        };

        dto.ComboOptions.Should().Equal("Active", "Inactive", "Pending");
    }

    [Fact]
    public void ComboOptions_is_empty_when_combo_values_is_null()
    {
        var dto = new ProcedureParameterDto
        {
            Id = 1,
            Caption = "Status",
            Name = "@Status",
            ComboValues = null
        };

        dto.ComboOptions.Should().BeEmpty();
    }

    [Fact]
    public void ComboOptions_is_empty_when_combo_values_is_malformed()
    {
        var dto = new ProcedureParameterDto
        {
            Id = 1,
            Caption = "Status",
            Name = "@Status",
            ComboValues = "not json"
        };

        dto.ComboOptions.Should().BeEmpty();
    }
}
