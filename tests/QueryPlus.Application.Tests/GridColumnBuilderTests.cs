using System.Data;
using FluentAssertions;
using QueryPlus.Application.Services;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.Tests;

public class GridColumnBuilderTests
{
    private readonly GridColumnBuilder _sut = new();

    [Fact]
    public void BuildGridColumns_MapsConfiguredColumnsAndFallbacks()
    {
        var procedure = new Procedure
        {
            IdProcedure = 1,
            IdCategory = 1,
            Caption = "Test SP",
            DatabaseName = "DB",
            ProcedureName = "sp_test",
            RoleEntitlement = "",
            Columns = new List<ProcedureColumn>
            {
                new()
                {
                    IdProcedureColumn = 10,
                    TechnicalName = "IdCustomer",
                    Caption = "Customer ID",
                    Alignment = ColumnAlignment.Right,
                    Visible = true
                }
            }
        };

        var dt = new DataTable();
        dt.Columns.Add("IdCustomer", typeof(int));
        dt.Columns.Add("UnconfiguredCol", typeof(string));

        var result = _sut.BuildGridColumns(procedure, dt);

        result.Should().HaveCount(2);
        result[0].TechnicalName.Should().Be("IdCustomer");
        result[0].Caption.Should().Be("Customer ID");
        result[0].Alignment.Should().Be(ColumnAlignment.Right);

        result[1].TechnicalName.Should().Be("UnconfiguredCol");
        result[1].Caption.Should().Be("UnconfiguredCol");
        result[1].Alignment.Should().Be(ColumnAlignment.Left);
    }
}
