using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using QueryPlus.Api.Tests.Infrastructure;
using QueryPlus.Application.Common;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Api.Tests.Integration;

public sealed class ProblemDetailsTests(QueryPlusApiApplicationFactory factory)
    : IClassFixture<QueryPlusApiApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task Validation_exception_returns_rfc7807_validation_problem()
    {
        factory.Execution.ExecuteAsync(Arg.Any<ExecuteProcedureRequest>(), Arg.Any<CancellationToken>())
            .Returns<ExecutionResultDto>(_ =>
                throw new ValidationException(new Dictionary<string, string[]> { ["Param"] = ["required"] }));

        var procedure = new Procedure
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
        };
        factory.ProcedureRepository.GetEnabledByIdWithDetailsAsync(7, Arg.Any<CancellationToken>()).Returns(procedure);

        var token = await AntiforgeryApiHelper.GetTokenAsync(_client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/execute")
        {
            Content = JsonContent.Create(new { procedureId = 7, parameterValues = new Dictionary<string, string?>() })
        };
        request.Headers.TryAddWithoutValidation(AntiforgeryApiHelper.CsrfHeaderName, token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().Contain("Validation failed");
        raw.Should().Contain("errors");
    }

    [Fact]
    public async Task Unhandled_exception_returns_generic_500_problem()
    {
        factory.Categories.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<CategoryDetailDto?>(_ => throw new InvalidOperationException("kaboom"));

        var token = await AntiforgeryApiHelper.GetTokenAsync(_client);
        var response = await _client.GetAsync("/api/categories/1");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("type").GetString().Should().StartWith("https://tools.ietf.org/html/rfc9110");
        json.GetProperty("title").GetString().Should().Be("An unexpected error occurred");
        json.TryGetProperty("detail", out _).Should().BeFalse();
    }
}
