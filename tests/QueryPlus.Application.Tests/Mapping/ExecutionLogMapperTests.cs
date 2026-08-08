using FluentAssertions;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.Mapping;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Application.Tests;

public class ExecutionLogMapperTests
{
    private static ExecutionLog BuildLog(bool withProcedure = true) => new()
    {
        IdExecutionLog = 42,
        IdProcedure = 7,
        ConnectionName = "DefaultConnection",
        Username = "alice",
        IpAddress = "10.0.0.1",
        ExecutionStart = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
        ExecutionEnd = new DateTime(2026, 1, 2, 3, 5, 5, DateTimeKind.Utc),
        Success = true,
        ErrorMessage = null,
        ParameterValues = "{\"@X\":\"1\"}",
        RowCount = 10,
        Procedure = withProcedure
            ? new Procedure { IdProcedure = 7, Caption = "Sales", ConnectionName = "DefaultConnection", DatabaseName = "D", ProcedureName = "p", RoleEntitlement = "user" }
            : null!
    };

    [Fact]
    public void ToDto_maps_all_fields()
    {
        var dto = ExecutionLogMapper.ToDto(BuildLog());

        dto.Id.Should().Be(42);
        dto.ProcedureId.Should().Be(7);
        dto.Username.Should().Be("alice");
        dto.IpAddress.Should().Be("10.0.0.1");
        dto.ExecutionStart.Should().NotBe(default);
        dto.ExecutionEnd.Should().NotBeNull();
        dto.Success.Should().BeTrue();
        dto.ErrorMessage.Should().BeNull();
        dto.ParameterValuesJson.Should().Be("{\"@X\":\"1\"}");
        dto.RowCount.Should().Be(10);
    }

    [Fact]
    public void ToListItemDto_includes_procedure_caption_when_nav_loaded()
    {
        var dto = ExecutionLogMapper.ToListItemDto(BuildLog());

        dto.ProcedureCaption.Should().Be("Sales");
    }

    [Fact]
    public void ToListItemDto_falls_back_to_empty_string_when_nav_is_null()
    {
        var dto = ExecutionLogMapper.ToListItemDto(BuildLog(withProcedure: false));

        dto.ProcedureCaption.Should().BeEmpty();
    }

    [Fact]
    public void Collection_overloads_materialize_arrays()
    {
        var logs = new[] { BuildLog(), BuildLog() };

        ExecutionLogMapper.ToDtos(logs).Should().HaveCount(2);
        ExecutionLogMapper.ToListItemDtos(logs).Should().HaveCount(2);
    }
}
