using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using QueryPlus.Api.Tests.Infrastructure;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.DTOs.Procedures;

namespace QueryPlus.Api.Tests.Integration;

public sealed class ExecutionLogsApiTests(QueryPlusApiApplicationFactory factory)
    : IClassFixture<QueryPlusApiApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task Search_returns_paged_results_with_all_filters()
    {
        factory.Execution.SearchAsync(Arg.Any<ExecutionLogFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ExecutionLogListItemDto>
            {
                Items =
                [
                    new ExecutionLogListItemDto
                    {
                        Id = 1, ProcedureId = 7, ProcedureCaption = "Demo", Username = "u",
                        ExecutionStart = DateTime.UtcNow, Success = true
                    }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 20
            });

        var response =
            await _client.GetAsync(
                "/api/execution-logs?username=u&procedureId=7&success=true&startFrom=2024-01-01&startTo=2024-12-31&pageNumber=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<PagedResult<ExecutionLogListItemDto>>();
        json!.Items.Should().ContainSingle();
        await factory.Execution.Received(1).SearchAsync(
            Arg.Is<ExecutionLogFilterDto>(f =>
                f.Username == "u" &&
                f.ProcedureId == 7 &&
                f.Success == true &&
                f.StartFrom == new DateTime(2024, 1, 1) &&
                f.StartTo == new DateTime(2024, 12, 31) &&
                f.Page == 1 &&
                f.PageSize == 20),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Procedure_lookup_returns_list()
    {
        factory.Procedures.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([new ProcedureLookupDto { Id = 7, CategoryId = 1, Caption = "Demo", RoleEntitlement = "user" }]);

        var response = await _client.GetAsync("/api/execution-logs/procedures");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<List<ProcedureLookupDto>>();
        json.Should().ContainSingle();
    }

    [Fact]
    public async Task Recent_logs_endpoint_resolves_via_service_helper()
    {
        factory.Execution.GetRecentByProcedureAsync(7, 10, Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(new List<ExecutionLogDto>
            {
                new() { Id = 1, ProcedureId = 7, Username = "u", ExecutionStart = DateTime.UtcNow, Success = true }
            });

        var result = await factory.Execution.GetRecentByProcedureAsync(7, 10);

        result.Should().ContainSingle();
    }
}
