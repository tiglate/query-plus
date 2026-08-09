using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using QueryPlus.Api.Tests.Infrastructure;

namespace QueryPlus.Api.Tests.Integration;

public sealed class AuthAndDefaultDenyTests(QueryPlusApiApplicationFactory factory)
    : IClassFixture<QueryPlusApiApplicationFactory>
{
    private readonly QueryPlusApiApplicationFactory _factory = factory;

    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task Anonymous_health_returns_ok()
    {
        var response = await _client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("healthy");
    }

    [Fact]
    public async Task Anonymous_health_ready_returns_503_when_database_is_unreachable()
    {
        // The factory points ConnectionStrings:DefaultConnection at an unreachable host
        // (Server=127.0.0.1,1) on purpose, so this proves /api/health/ready does a real
        // connectivity check rather than always reporting healthy like /api/health.
        var response = await _client.GetAsync("/api/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("unhealthy");
    }

    [Fact]
    public async Task Anonymous_csrf_returns_token_with_cookie()
    {
        var response = await _client.GetAsync(AntiforgeryApiHelper.CsrfEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
        response.Headers.Should().Contain(h => h.Key == "Set-Cookie"
                                               && h.Value.Any(v =>
                                                   v.StartsWith($"{AntiforgeryApiHelper.CsrfCookieName}=",
                                                       StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task Authenticated_user_returns_claims()
    {
        var response = await _client.GetAsync("/api/auth/user");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("username").GetString().Should().Be("test-user");
        json.GetProperty("isAuthenticated").GetBoolean().Should().BeTrue();
        json.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).Should().Contain(["ROLE_ADMIN"]);
    }

    [Fact]
    public async Task Spa_fallback_for_unknown_route_returns_file_or_api_404()
    {
        var response = await _client.GetAsync("/some/spa/route/that/does/not/exist");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Json_post_without_csrf_is_rejected()
    {
        var content = JsonContent.Create(new { description = "Test" });
        var response = await _client.PostAsync("/api/categories", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Json_post_with_invalid_csrf_is_rejected()
    {
        var content = JsonContent.Create(new { description = "Test" });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/categories") { Content = content };
        request.Headers.TryAddWithoutValidation(AntiforgeryApiHelper.CsrfHeaderName, "invalid-token-value");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
