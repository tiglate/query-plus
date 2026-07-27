using FluentAssertions;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Mapping;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.Tests;

public class ProcedureGraphMapperTests
{
    [Fact]
    public void ToNewEntity_MapsAllMasterDetailFields()
    {
        var dto = new SaveProcedureDto
        {
            CategoryId = 1,
            Caption = " New Sales ",
            DatabaseName = " DB_Sales ",
            ProcedureName = " sp_Sales ",
            Enabled = true,
            SupportsPagination = true,
            RoleEntitlement = " user_role ",
            Description = " Detailed description ",
            Parameters = new List<SaveProcedureParameterDto>
            {
                new()
                {
                    Caption = " Start Date ",
                    Name = " StartDate ",
                    ParameterType = ParameterType.Date,
                    DefaultValue = " 2026-01-01 ",
                    IsRequired = true,
                    IsSensitive = false
                }
            },
            Columns = new List<SaveProcedureColumnDto>
            {
                new()
                {
                    TechnicalName = " TotalAmount ",
                    Caption = " Total Amount ",
                    Alignment = ColumnAlignment.Right,
                    FormatMask = " C2 ",
                    Visible = true
                }
            }
        };

        var entity = ProcedureGraphMapper.ToNewEntity(dto);

        entity.Caption.Should().Be("New Sales");
        entity.DatabaseName.Should().Be("DB_Sales");
        entity.ProcedureName.Should().Be("sp_Sales");
        entity.RoleEntitlement.Should().Be("user_role");
        entity.Description.Should().Be("Detailed description");
        entity.Parameters.Should().HaveCount(1);
        entity.Parameters.First().Name.Should().Be("@StartDate");
        entity.Columns.Should().HaveCount(1);
        entity.Columns.First().TechnicalName.Should().Be("TotalAmount");
    }

    [Fact]
    public void ApplyUpdate_SynchronizesParametersAndColumns()
    {
        var existingParam = new ProcedureParameter { IdProcedureParameter = 10, Name = "@Old", Caption = "Old", ParameterType = ParameterType.FreeText };
        var existingCol = new ProcedureColumn { IdProcedureColumn = 20, TechnicalName = "OldCol", Caption = "OldCol", Alignment = ColumnAlignment.Left };

        var entity = new Procedure
        {
            IdProcedure = 1,
            IdCategory = 1,
            Caption = "Old Cap",
            DatabaseName = "DB",
            ProcedureName = "sp_old",
            RoleEntitlement = "",
            Parameters = new List<ProcedureParameter> { existingParam },
            Columns = new List<ProcedureColumn> { existingCol }
        };

        var dto = new SaveProcedureDto
        {
            Id = 1,
            CategoryId = 2,
            Caption = "Updated Cap",
            DatabaseName = "DB",
            ProcedureName = "sp_old",
            RoleEntitlement = "admin",
            Parameters = new List<SaveProcedureParameterDto>
            {
                new() { Id = 10, Caption = "Updated Param", Name = "Old", ParameterType = ParameterType.Numeric, IsRequired = true }
            },
            Columns = new List<SaveProcedureColumnDto>
            {
                new() { Id = 20, TechnicalName = "OldCol", Caption = "Updated Col", Alignment = ColumnAlignment.Right, Visible = true }
            }
        };

        ProcedureGraphMapper.ApplyUpdate(entity, dto);

        entity.Caption.Should().Be("Updated Cap");
        entity.Parameters.Should().HaveCount(1);
        entity.Parameters.First().Caption.Should().Be("Updated Param");
        entity.Columns.Should().HaveCount(1);
        entity.Columns.First().Caption.Should().Be("Updated Col");
    }
}
