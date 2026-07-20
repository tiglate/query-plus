using FluentAssertions;
using FluentValidation.TestHelper;
using QueryPlus.Application.Common;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Validation;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.Tests;

public sealed class ProcedurePaginationTests
{
    [Theory]
    [InlineData("@PageNumber")]
    [InlineData("PageNumber")]
    [InlineData("@pagesize")]
    [InlineData("TotalRecords")]
    [InlineData("@TotalRecords")]
    public void IsReservedParameterName_detects_reserved(string name)
        => Assert.True(ProcedurePagination.IsReservedParameterName(name));

    [Theory]
    [InlineData("@StartDate")]
    [InlineData("CustomerId")]
    [InlineData("")]
    [InlineData(null)]
    public void IsReservedParameterName_allows_normal_names(string? name)
        => Assert.False(ProcedurePagination.IsReservedParameterName(name));

    [Fact]
    public void ClampPageNumber_and_UiPageSize()
    {
        Assert.Equal(1, ProcedurePagination.ClampPageNumber(null));
        Assert.Equal(1, ProcedurePagination.ClampPageNumber(0));
        Assert.Equal(3, ProcedurePagination.ClampPageNumber(3));

        Assert.Equal(50, ProcedurePagination.ClampUiPageSize(null));
        Assert.Equal(50, ProcedurePagination.ClampUiPageSize(0));
        Assert.Equal(100, ProcedurePagination.ClampUiPageSize(100));
        Assert.Equal(200, ProcedurePagination.ClampUiPageSize(500));
    }

    [Fact]
    public void WithPagingInputs_adds_page_args()
    {
        var user = new Dictionary<string, object?> { ["@Foo"] = "bar" };
        var bound = ProcedurePagination.WithPagingInputs(user, 2, 50);
        Assert.Equal("bar", bound["@Foo"]);
        Assert.Equal(2L, bound[ProcedurePagination.PageNumberName]);
        Assert.Equal(50L, bound[ProcedurePagination.PageSizeName]);
    }

    [Fact]
    public void ExportPageSize_is_product_convention()
        => Assert.Equal(999_999_999L, ProcedurePagination.ExportPageSize);

    [Fact]
    public void CommandTimeout_is_30_minutes()
        => Assert.Equal(1800, ProcedurePagination.CommandTimeoutSeconds);

    [Theory]
    [InlineData("@PageNumber")]
    [InlineData("@PageSize")]
    [InlineData("@TotalRecords")]
    public void SaveProcedure_rejects_reserved_parameter_names(string reservedName)
    {
        var dto = new SaveProcedureDto
        {
            CategoryId = 1,
            Caption = "Test",
            DatabaseName = "QueryPlus",
            ProcedureName = "dbo.usp_Test",
            RoleEntitlement = "user",
            Parameters =
            [
                new SaveProcedureParameterDto
                {
                    Caption = "Reserved",
                    Name = reservedName,
                    ParameterType = ParameterType.Numeric
                }
            ]
        };

        var result = new SaveProcedureDtoValidator().TestValidate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage.Contains("reserved", StringComparison.OrdinalIgnoreCase));
    }
}
