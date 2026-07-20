using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Domain.Entities;
using QueryPlus.Web.Tests.Infrastructure;

namespace QueryPlus.Web.Tests.Integration;

/// <summary>
/// Integration tests for Home HTMX-style POST endpoints (Execute / Export / Parameters)
/// including antiforgery token handling matching the browser/ClientApp flow.
/// </summary>
public sealed class HomeHtmxEndpointTests : IClassFixture<QueryPlusWebApplicationFactory>
{
    private readonly QueryPlusWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HomeHtmxEndpointTests(QueryPlusWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _factory.Procedures.GetAccessibleForCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(
            [
                new ProcedureLookupDto
                {
                    Id = 7,
                    CategoryId = 1,
                    Caption = "Demo",
                    RoleEntitlement = "user"
                }
            ]);
    }

    [Fact]
    public async Task Parameters_with_valid_id_returns_parameter_form_partial()
    {
        _factory.Procedures.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(new ProcedureDetailDto
            {
                Id = 7,
                CategoryId = 1,
                Caption = "Demo",
                DatabaseName = "db",
                ProcedureName = "dbo.usp_Demo",
                RoleEntitlement = "user",
                Enabled = true,
                Parameters =
                [
                    new ProcedureParameterDto
                    {
                        Id = 1,
                        Caption = "Start",
                        Name = "@Start",
                        ParameterType = Domain.Enums.ParameterType.Date,
                        IsRequired = false
                    }
                ]
            });

        var response = await _client.GetAsync("/Home/Parameters?procedureId=7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Start");
        body.Should().Contain("param_Start");
    }

    [Fact]
    public async Task Execute_without_procedure_returns_error_fragment()
    {
        var token = await AntiforgeryTestHelper.GetRequestTokenAsync(_client, "/");
        using var request = AntiforgeryTestHelper.CreateFormPost(
            "/Home/Execute",
            token,
            [new KeyValuePair<string, string>("pageNumber", "1")]);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("js-results-root");
        // Localized select-procedure message (pt-BR default) or English key content
        (body.Contains("procedure", StringComparison.OrdinalIgnoreCase)
         || body.Contains("Selecione", StringComparison.OrdinalIgnoreCase)
         || body.Contains("Select", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }

    [Fact]
    public async Task Execute_with_valid_procedure_returns_results_grid()
    {
        _factory.ProcedureRepository.GetEnabledByIdWithDetailsAsync(7, Arg.Any<CancellationToken>())
            .Returns(new Procedure
            {
                IdProcedure = 7,
                IdCategory = 1,
                Caption = "Demo",
                DatabaseName = "db",
                ProcedureName = "dbo.usp_Demo",
                RoleEntitlement = "user",
                Enabled = true,
                Parameters = [],
                Columns = []
            });

        _factory.Execution.ExecuteAsync(Arg.Any<ExecuteProcedureRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ExecutionResultDto
            {
                Success = true,
                ProcedureId = 7,
                ProcedureCaption = "Demo",
                RowCount = 0,
                Columns = []
            });

        var token = await AntiforgeryTestHelper.GetRequestTokenAsync(_client, "/");
        using var request = AntiforgeryTestHelper.CreateFormPost(
            "/Home/Execute",
            token,
            [
                new KeyValuePair<string, string>("procedureId", "7"),
                new KeyValuePair<string, string>("pageNumber", "1"),
                new KeyValuePair<string, string>("pageSize", "50")
            ]);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("js-results-root");
        await _factory.Execution.Received(1)
            .ExecuteAsync(
                Arg.Is<ExecuteProcedureRequest>(r => r.ProcedureId == 7),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_without_antiforgery_form_field_is_rejected()
    {
        // Cookie from GET / alone is not enough — body must carry a matching token.
        _ = await _client.GetAsync("/");
        using var content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("procedureId", "7")
        ]);

        var response = await _client.PostAsync("/Home/Execute", content);

        // Missing/invalid antiforgery must not execute the action successfully.
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        await _factory.Execution.DidNotReceive()
            .ExecuteAsync(Arg.Any<ExecuteProcedureRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Export_without_prior_execute_returns_error_message()
    {
        var token = await AntiforgeryTestHelper.GetRequestTokenAsync(_client, "/");
        using var request = AntiforgeryTestHelper.CreateFormPost(
            "/Home/Export",
            token,
            [new KeyValuePair<string, string>("procedureId", "7")]);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("text-red");
    }

    [Fact]
    public async Task ExportStatus_returns_partial_html()
    {
        var jobId = Guid.NewGuid();
        var response = await _client.GetAsync($"/Home/ExportStatus?jobId={jobId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();
    }
}
