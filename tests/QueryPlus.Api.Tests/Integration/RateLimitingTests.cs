using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using QueryPlus.Api.Tests.Infrastructure;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Api.Tests.Integration;

/// <summary>
/// Regression coverage for the per-user concurrency limit on /api/execute
/// (RateLimitingServiceCollectionExtensions.ExecutePolicy): a single user firing more
/// concurrent executions than the permit limit must be throttled with 429, not allowed
/// to pile up unbounded long-running SQL work.
/// </summary>
public sealed class RateLimitingTests(QueryPlusApiApplicationFactory factory)
    : IClassFixture<QueryPlusApiApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task Concurrent_executes_beyond_the_permit_limit_are_throttled_with_429()
    {
        const int procedureId = 501;
        factory.ProcedureRepository.GetEnabledByIdWithDetailsAsync(procedureId, Arg.Any<CancellationToken>())
            .Returns(new Procedure
            {
                IdProcedure = procedureId,
                IdCategory = 1,
                Caption = "Slow",
                DatabaseName = "db",
                ProcedureName = "dbo.usp_Slow",
                RoleEntitlement = "user",
                Enabled = true,
                Parameters = [],
                Columns = []
            });
        factory.Execution.ExecuteAsync(Arg.Any<ExecuteProcedureRequest>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                return new ExecutionResultDto { Success = true, ProcedureId = procedureId, RowCount = 0 };
            });

        var token = await AntiforgeryApiHelper.GetTokenAsync(_client);

        HttpRequestMessage BuildRequest() => AntiforgeryApiHelper.CreateJsonPost("/api/execute", token,
            JsonContent.Create(new { procedureId, parameterValues = new Dictionary<string, string?>() }));

        // The ExecutePolicy concurrency limiter permits 3 simultaneous in-flight executions
        // per user; firing 5 at once against a handler that holds each open for 500ms must
        // reject the overflow instead of letting all 5 pile up against SQL Server.
        var responses = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => _client.SendAsync(BuildRequest())));

        responses.Select(r => r.StatusCode)
            .Should().Contain(HttpStatusCode.TooManyRequests,
                "the concurrency limiter should reject at least one of 5 simultaneous executions");
        responses.Select(r => r.StatusCode)
            .Should().Contain(HttpStatusCode.OK, "requests within the permit limit should still succeed");
    }
}
