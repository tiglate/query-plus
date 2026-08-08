using FluentAssertions;
using QueryPlus.Application.Common;
using QueryPlus.Application.Services;
using QueryPlus.Application.Services.Converters;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.Tests;

public class ExecutionParameterResolverTests
{
    private readonly ExecutionParameterResolver _sut = new(ParameterConverterRegistry.CreateDefault());

    private static Procedure MakeProcedure(bool supportsPagination, params ProcedureParameter[] parameters) => new()
    {
        IdProcedure = 1,
        IdCategory = 1,
        Caption = "Test Proc",
        DatabaseName = "db",
        ProcedureName = "sp_test",
        RoleEntitlement = "",
        SupportsPagination = supportsPagination,
        Parameters = parameters
    };

    private static ProcedureParameter MakeFreeTextParam(string name, string caption = "Category") => new()
    {
        IdProcedureParameter = 1,
        Name = name,
        Caption = caption,
        ParameterType = ParameterType.FreeText
    };

    [Fact]
    public void Resolve_WhenPaginationNotSupported_DoesNotInjectPagingInputs()
    {
        var procedure = MakeProcedure(supportsPagination: false, MakeFreeTextParam("@Category"));
        var rawValues = new Dictionary<string, string?> { ["@Category"] = "Sales" };

        var result = _sut.Resolve(procedure, rawValues, requestedPageNumber: 3, requestedPageSize: 10);

        result.ExecParameters.Should().BeSameAs(result.BoundUserParameters);
        result.ExecParameters.Should().NotContainKey(ProcedurePagination.PageNumberName);
        result.ExecParameters.Should().NotContainKey(ProcedurePagination.PageSizeName);
        result.OutputParameterNames.Should().BeNull();
        result.PageNumber.Should().Be(ProcedurePagination.DefaultPageNumber);
        result.PageSize.Should().Be(ProcedurePagination.DefaultPageSize);
    }

    [Fact]
    public void Resolve_WhenPaginationSupported_InjectsClampedPagingInputsAndOutputName()
    {
        var procedure = MakeProcedure(supportsPagination: true, MakeFreeTextParam("@Category"));
        var rawValues = new Dictionary<string, string?> { ["@Category"] = "Sales" };

        var result = _sut.Resolve(procedure, rawValues, requestedPageNumber: 3, requestedPageSize: 500);

        result.PageNumber.Should().Be(3);
        result.PageSize.Should().Be(ProcedurePagination.MaxUiPageSize); // 500 clamped down to 200
        result.ExecParameters[ProcedurePagination.PageNumberName].Should().Be(3L);
        result.ExecParameters[ProcedurePagination.PageSizeName].Should().Be(ProcedurePagination.MaxUiPageSize);
        result.ExecParameters["@Category"].Should().Be("Sales");
        result.OutputParameterNames.Should().BeEquivalentTo([ProcedurePagination.TotalRecordsName]);
    }

    [Fact]
    public void Resolve_WhenPaginationSupported_AndNoPageRequested_UsesDefaults()
    {
        var procedure = MakeProcedure(supportsPagination: true, MakeFreeTextParam("@Category"));
        var rawValues = new Dictionary<string, string?> { ["@Category"] = "Sales" };

        var result = _sut.Resolve(procedure, rawValues, requestedPageNumber: null, requestedPageSize: null);

        result.PageNumber.Should().Be(ProcedurePagination.DefaultPageNumber);
        result.PageSize.Should().Be(ProcedurePagination.DefaultPageSize);
    }

    [Fact]
    public void Resolve_FiltersOutReservedParameterNames_BeforeBinding()
    {
        var procedure = MakeProcedure(
            supportsPagination: true,
            MakeFreeTextParam("@Category"),
            MakeFreeTextParam("@PageNumber", "Page Number")); // catalog data should never define this, but guard anyway
        var rawValues = new Dictionary<string, string?>
        {
            ["@Category"] = "Sales",
            ["@PageNumber"] = "999" // attacker/UI-supplied attempt to override system-injected paging
        };

        var result = _sut.Resolve(procedure, rawValues, requestedPageNumber: 1, requestedPageSize: 10);

        result.BoundUserParameters.Should().ContainKey("@Category");
        result.BoundUserParameters.Should().NotContainKey("@PageNumber");
        result.ExecParameters["@PageNumber"].Should().Be(1L); // system-injected value, not the user-supplied 999
    }
}
